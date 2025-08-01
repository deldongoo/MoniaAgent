using GitLabApiClient;
using GitLabApiClient.Models.Branches.Requests;
using GitLabApiClient.Models.Commits.Requests;
using GitLabApiClient.Models.Commits.Requests.CreateCommitRequest;
using GitLabApiClient.Models.Issues.Requests;
using GitLabApiClient.Models.MergeRequests.Requests;
using GitLabApiClient.Models.Notes.Requests;
using GitLabApiClient.Models.Projects.Requests;
using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using MoniaAgent.Configuration;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MoniaAgentTest.Agents
{
    public class GitLabOutput : AgentOutput
    {
        public string Operation { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? ProjectId { get; set; }
        public string? Branch { get; set; }
        public string? CommitSha { get; set; }
    }

    [AgentMetadata(
        Name = "GitLabAgent",
        Specialty = "GitLab repository management and operations",
        Keywords = new[] {
            "git", "gitlab", "repository", "commit", "push", "file", "create", "update",
            "issue", "merge", "request", "branch", "search", "project", "repo", "version", "control"
        },
        Goal = @"You are a GitLab specialist agent with access to GitLab API operations.
                    You can:
                    - Create and update files in repositories
                    - Push multiple files in a single commit
                    - Search for repositories
                    - Get file and directory contents
                    - Create issues
                    - Create merge requests
                    - Read and handle issues
                    - Search code in repositories 
                    - Create working branches
                    - Update issues
                    - Create issue notes

                    When asked to ""treat"" or ""handle"" an issue:
                    1. First use get_issue to understand the issue details
                    2. Analyze the issue description to identify what needs to be changed
                    3. Use search_code_in_repository to find relevant files containing the code to modify. 
                    4. Use get_file_contents to examine the exact content of identified files. If you're not satisfied with what you found, use search_code_in_repository again with a new search query. 
                    5. Create a new branch for the fix using create_branch (format: fix/issue-{number}-{short-description})
                    6. Make the necessary changes using create_or_update_file
                    7. Create a merge request linking to the issue using create_merge_request
                    8. Add a comment on the issue using create_issue_note to indicate the MR has been created
                    9. Optionally update the issue status using update_issue

                    For code changes described in issues:
                    - Search intelligently using keywords from the issue description
                    - If searching for numeric values (like ""500""), also search for context words nearby
                    - Examine multiple files if the first search doesn't yield results
                    - Look for common file patterns (config files, animation files, component files)
                    
                    Use the available GitLab tools to complete user requests efficiently."
    )]
    public class GitLabAgent : TypedAgent<TextInput, GitLabOutput>
    {
        private readonly string accessToken;
        private readonly string apiUrl;
        private GitLabClient? gitLabClient;

        public GitLabAgent(LLM llm, string accessToken, string apiUrl) : base(llm)
        {
            this.accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            this.apiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
            InitializeGitLabClient();
        }

        private void InitializeGitLabClient()
        {
            try
            {
                gitLabClient = new GitLabClient(apiUrl, accessToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize GitLab client: {ex.Message}", ex);
            }
        }

    protected override AgentConfig ConfigureTools() => new()
    {
        ToolMethods = new Delegate[] {
                CreateOrUpdateFile,
                PushFiles,
                SearchRepositories,
                GetFileContents,
                CreateIssue,
                CreateMergeRequest,
                GetIssue,
                SearchCodeInRepository,
                CreateBranch,
                UpdateIssue,
                CreateIssueNote
            }
    };

        private async Task<GitLabOutput> CreateOrUpdateFile(
    string projectId,
    string filePath,
    string content,
    string commitMessage,
    string branch,
    string? previousPath = null)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                var actions = new List<CreateCommitRequestAction>();

                if (!String.IsNullOrEmpty(previousPath) && previousPath != null)
                {
                    // Move operation
                    actions.Add(new CreateCommitRequestAction(CreateCommitRequestActionType.Move, filePath)
                    {
                        PreviousPath = previousPath,
                        Content = content
                    });
                }
                else
                {
                    // Pour une branche nouvellement créée, le fichier existe toujours (copié depuis la branche source)
                    // On doit donc toujours utiliser Update sauf si on sait que c'est un nouveau fichier
                    // La façon la plus simple est d'essayer Update d'abord, et si ça échoue, essayer Create

                    // Ou plus simplement : toujours utiliser Update pour les fichiers existants
                    // GitLab accepte Update même sur une nouvelle branche
                    actions.Add(new CreateCommitRequestAction(CreateCommitRequestActionType.Update, filePath)
                    {
                        Content = content
                    });
                }

                var createRequest = new CreateCommitRequest(branch, commitMessage, actions);
                var commit = await gitLabClient.Commits.CreateAsync(projectId, createRequest);

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "CreateOrUpdateFile",
                    Content = $"File {filePath} successfully {(previousPath != null ? "moved and updated" : "updated")} in project {projectId}",
                    Data = new
                    {
                        CommitId = commit.Id,
                        CommitMessage = commit.Message,
                        AuthorName = commit.AuthorName,
                        CreatedAt = commit.CreatedAt,
                        FilePath = filePath,
                        Branch = branch
                    },
                    ProjectId = projectId,
                    Branch = branch,
                    CommitSha = commit.Id
                };
            }
            catch (Exception ex)
            {
                // Si Update échoue (fichier n'existe pas), essayer Create
                if (ex.Message.Contains("does not exist") || ex.Message.Contains("404"))
                {
                    try
                    {
                        var actions = new List<CreateCommitRequestAction>
                {
                    new CreateCommitRequestAction(CreateCommitRequestActionType.Create, filePath)
                    {
                        Content = content
                    }
                };

                        var createRequest = new CreateCommitRequest(branch, commitMessage, actions);
                        var commit = await gitLabClient.Commits.CreateAsync(projectId, createRequest);

                        return new GitLabOutput
                        {
                            Success = true,
                            Operation = "CreateOrUpdateFile",
                            Content = $"File {filePath} successfully created in project {projectId}",
                            Data = new
                            {
                                CommitId = commit.Id,
                                CommitMessage = commit.Message,
                                AuthorName = commit.AuthorName,
                                CreatedAt = commit.CreatedAt,
                                FilePath = filePath,
                                Branch = branch
                            },
                            ProjectId = projectId,
                            Branch = branch,
                            CommitSha = commit.Id
                        };
                    }
                    catch (Exception innerEx)
                    {
                        return new GitLabOutput
                        {
                            Success = false,
                            Operation = "CreateOrUpdateFile",
                            ErrorMessage = innerEx.Message,
                            Content = $"Failed to create/update file {filePath}: {innerEx.Message}"
                        };
                    }
                }

                return new GitLabOutput
                {
                    Success = false,
                    Operation = "CreateOrUpdateFile",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to create/update file {filePath}: {ex.Message}"
                };
            }
        }

        [Description("Push multiple files in a single commit to a GitLab project. Parameters: projectId (string), branch (string), files (array of objects with filePath and content), commitMessage (string)")]
        private async Task<GitLabOutput> PushFiles(
            string projectId,
            string branch,
            string filesJson,
            string commitMessage)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                // Parse files from JSON
                var files = JsonSerializer.Deserialize<List<FileItem>>(filesJson);
                if (files == null || !files.Any())
                    throw new ArgumentException("Files array cannot be empty");

                var actions = files.Select(file =>
                    new CreateCommitRequestAction(CreateCommitRequestActionType.Create, file.FilePath)
                    {
                        Content = file.Content
                    }).ToList();

                var createRequest = new CreateCommitRequest(branch, commitMessage, actions);

                var commit = await gitLabClient.Commits.CreateAsync(projectId, createRequest);

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "PushFiles",
                    Content = $"Successfully pushed {files.Count} files to branch {branch} in project {projectId}",
                    Data = new
                    {
                        CommitId = commit.Id,
                        CommitMessage = commit.Message,
                        AuthorName = commit.AuthorName,
                        CreatedAt = commit.CreatedAt,
                        FilesCount = files.Count,
                        Files = files.Select(f => f.FilePath).ToArray(),
                        Branch = branch
                    },
                    ProjectId = projectId,
                    Branch = branch,
                    CommitSha = commit.Id
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "PushFiles",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to push files: {ex.Message}"
                };
            }
        }

        [Description("Create a new branch in a GitLab project. Parameters: projectId (string), branchName (string), ref (string - source branch/tag/commit to branch from)")]
        private async Task<GitLabOutput> CreateBranch(
            string projectId,
            string branchName,
            string @ref)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                // Create the branch using the repository branches API
                var branch = await gitLabClient.Branches.CreateAsync(projectId, new CreateBranchRequest(branchName, @ref));

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "CreateBranch",
                    Content = $"Successfully created branch '{branchName}' from '{@ref}' in project {projectId}",
                    Data = new
                    {
                        BranchName = branch.Name,
                        Commit = new
                        {
                            Id = branch.Commit.Id,
                            ShortId = branch.Commit.ShortId,
                            Title = branch.Commit.Title,
                            Message = branch.Commit.Message,
                            AuthorName = branch.Commit.AuthorName,
                            AuthorEmail = branch.Commit.AuthorEmail,
                            AuthoredDate = branch.Commit.AuthoredDate,
                            CommitterName = branch.Commit.CommitterName,
                            CommitterEmail = branch.Commit.CommitterEmail,
                            CommittedDate = branch.Commit.CommittedDate
                        },
                        Protected = branch.Protected,
                        Merged = branch.Merged,
                        Default = branch.Default,
                        DevelopersCanPush = branch.DevelopersCanPush,
                        DevelopersCanMerge = branch.DevelopersCanMerge,
                    },
                    ProjectId = projectId,
                    Branch = branchName
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "CreateBranch",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to create branch '{branchName}': {ex.Message}"
                };
            }
        }

        [Description("Search for GitLab projects. Parameters: search (string), page (optional number), perPage (optional number, default 20)")]
        private async Task<GitLabOutput> SearchRepositories(string search, int page = 1, int perPage = 20)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                var projects = await gitLabClient.Projects.GetAsync(options =>
                {
                    options.Filter = search; // Correct property name
                    // Note: page and perPage are not available in ProjectQueryOptions
                    // The API handles pagination automatically with GetPagedList
                });

                var projectData = projects.Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    PathWithNamespace = p.PathWithNamespace,
                    Description = p.Description,
                    WebUrl = p.WebUrl,
                    LastActivity = p.LastActivityAt,
                    DefaultBranch = p.DefaultBranch,
                    Visibility = p.Visibility.ToString() // Remove nullable operator
                }).ToArray();

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "SearchRepositories",
                    Content = $"Found {projectData.Length} projects matching '{search}'",
                    Data = new
                    {
                        SearchQuery = search,
                        ResultsCount = projectData.Length,
                        Projects = projectData
                    }
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "SearchRepositories",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to search repositories: {ex.Message}"
                };
            }
        }

        [Description("Get contents of a file or directory from a GitLab project. Parameters: projectId (string), filePath (string), reference (optional string - branch/tag/commit, default 'master')")]
        private async Task<GitLabOutput> GetFileContents(string projectId, string filePath, string? reference = null)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                var file = await gitLabClient.Files.GetAsync(projectId, filePath, reference ?? "master");

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "GetFileContents",
                    Content = $"Retrieved contents of {filePath} from project {projectId}",
                    Data = new
                    {
                        FileName = file.Filename,
                        FilePath = file.FullPath,
                        Size = file.Size,
                        Encoding = file.Encoding,
                        Content = file.Content,
                        ContentDecoded = file.ContentDecoded, // Use the decoded property
                        ContentSha256 = file.ContentSha256,
                        Reference = reference ?? "master",
                        LastCommitId = file.LastCommitId,
                        BlobId = file.BlobId
                    },
                    ProjectId = projectId
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "GetFileContents",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to get file contents for {filePath}: {ex.Message}"
                };
            }
        }

        [Description("Search for code in a GitLab project repository. Parameters: projectId (string), searchQuery (string), branch (optional string - default 'master'), perPage (optional int - default 20)")]
        private async Task<GitLabOutput> SearchCodeInRepository(
                string projectId,
                string searchQuery,
                string? branch = null,
                int perPage = 20)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                // GitLab API doesn't have a direct code search endpoint in the client library
                // We'll use the raw HTTP client to access the search API
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", accessToken);

                var encodedQuery = Uri.EscapeDataString(searchQuery);
                var encodedProjectId = Uri.EscapeDataString(projectId);
                var branchParam = !string.IsNullOrEmpty(branch) ? $"&ref={Uri.EscapeDataString(branch)}" : "";

                var searchUrl = $"{apiUrl}/api/v4/projects/{encodedProjectId}/search?scope=blobs&search={encodedQuery}{branchParam}&per_page={perPage}";

                var response = await httpClient.GetAsync(searchUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var searchResults = JsonSerializer.Deserialize<List<CodeSearchResult>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (searchResults == null || !searchResults.Any())
                {
                    return new GitLabOutput
                    {
                        Success = true,
                        Operation = "SearchCodeInRepository",
                        Content = $"No results found for '{searchQuery}' in project {projectId}",
                        Data = new
                        {
                            SearchQuery = searchQuery,
                            Branch = branch ?? "all branches",
                            ResultsCount = 0,
                            Results = Array.Empty<object>()
                        },
                        ProjectId = projectId
                    };
                }

                var formattedResults = searchResults.Select(r => new
                {
                    FileName = r.Filename,
                    Path = r.Path,
                    Ref = r.Ref,
                    StartLine = r.Startline,
                    MatchedContent = r.Data,
                    ProjectId = r.ProjectId
                }).ToArray();

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "SearchCodeInRepository",
                    Content = $"Found {formattedResults.Length} results for '{searchQuery}' in project {projectId}",
                    Data = new
                    {
                        SearchQuery = searchQuery,
                        Branch = branch ?? "all branches",
                        ResultsCount = formattedResults.Length,
                        Results = formattedResults
                    },
                    ProjectId = projectId
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "SearchCodeInRepository",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to search code in repository: {ex.Message}"
                };
            }
        }

        // Helper class for code search results
        private class CodeSearchResult
        {
            public string? Basename { get; set; }
            public string? Data { get; set; }
            public string? Filename { get; set; }
            public int? Id { get; set; }
            public string? Ref { get; set; }
            public int? Startline { get; set; }
            public int? ProjectId { get; set; }
            public string? Path { get; set; }
        }

        [Description("Create a new issue in a GitLab project. Parameters: projectId (string), title (string), description (optional string), assigneeId (optional number - single user ID), labels (optional string - comma-separated labels), milestoneId (optional number)")]
        private async Task<GitLabOutput> CreateIssue(
            string projectId,
            string title,
            string? description = null,
            int? assigneeId = null,
            string? labels = null,
            int? milestoneId = null)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                var createRequest = new CreateIssueRequest(title)
                {
                    Description = description
                };

                if (assigneeId.HasValue)
                {
                    createRequest.Assignees = new List<int> { assigneeId.Value };
                }

                if (!string.IsNullOrEmpty(labels))
                {
                    createRequest.Labels = labels.Split(',').Select(l => l.Trim()).ToArray();
                }

                if (milestoneId.HasValue)
                {
                    createRequest.MilestoneId = milestoneId.Value;
                }

                var issue = await gitLabClient.Issues.CreateAsync(projectId, createRequest);

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "CreateIssue",
                    Content = $"Successfully created issue #{issue.Iid}: {title} in project {projectId}",
                    Data = new
                    {
                        IssueId = issue.Id,
                        IssueIid = issue.Iid,
                        Title = issue.Title,
                        Description = issue.Description,
                        State = issue.State.ToString(),
                        CreatedAt = issue.CreatedAt,
                        UpdatedAt = issue.UpdatedAt,
                        Author = issue.Author?.Name,
                        WebUrl = issue.WebUrl,
                        Labels = issue.Labels,
                        Assignees = issue.Assignees?.Select(a => a.Name).ToArray()
                    },
                    ProjectId = projectId
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "CreateIssue",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to create issue: {ex.Message}"
                };
            }
        }

        [Description("Create a new merge request in a GitLab project. Parameters: projectId (string), title (string), description (optional string), sourceBranch (string), targetBranch (string), draft (optional boolean)")]
        private async Task<GitLabOutput> CreateMergeRequest(
            string projectId,
            string title,
            string? description,
            string sourceBranch,
            string targetBranch,
            bool draft = false)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                var createRequest = new CreateMergeRequest(sourceBranch, targetBranch, title)
                {
                    Description = description
                    // Note: AllowCollaboration property doesn't exist in this version
                };

                // Handle draft status by prefixing title if needed
                string finalTitle = title;
                if (draft && !title.StartsWith("Draft:", StringComparison.OrdinalIgnoreCase))
                {
                    finalTitle = $"Draft: {title}";
                    // Create a new request with the draft title since Title is read-only
                    createRequest = new CreateMergeRequest(sourceBranch, targetBranch, finalTitle)
                    {
                        Description = description
                    };
                }

                var mergeRequest = await gitLabClient.MergeRequests.CreateAsync(projectId, createRequest);

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "CreateMergeRequest",
                    Content = $"Successfully created merge request !{mergeRequest.Iid}: {title} in project {projectId}",
                    Data = new
                    {
                        MergeRequestId = mergeRequest.Id,
                        MergeRequestIid = mergeRequest.Iid,
                        Title = mergeRequest.Title,
                        Description = mergeRequest.Description,
                        State = mergeRequest.State.ToString(),
                        SourceBranch = mergeRequest.SourceBranch,
                        TargetBranch = mergeRequest.TargetBranch,
                        CreatedAt = mergeRequest.CreatedAt,
                        UpdatedAt = mergeRequest.UpdatedAt,
                        Author = mergeRequest.Author?.Name,
                        WebUrl = mergeRequest.WebUrl,
                        Draft = mergeRequest.Title.StartsWith("Draft:", StringComparison.OrdinalIgnoreCase)
                        // Note: AllowCollaboration not available in this API version
                    },
                    ProjectId = projectId,
                    Branch = sourceBranch
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "CreateMergeRequest",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to create merge request: {ex.Message}"
                };
            }
        }

        [Description("Get details of a specific issue in a GitLab project. Parameters: projectId (string), issueIid (int - the internal issue ID)")]
        private async Task<GitLabOutput> GetIssue(string projectId, int issueIid)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                var issue = await gitLabClient.Issues.GetAsync(projectId, issueIid);

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "GetIssue",
                    Content = $"Retrieved issue #{issue.Iid}: {issue.Title} from project {projectId}",
                    Data = new
                    {
                        IssueId = issue.Id,
                        IssueIid = issue.Iid,
                        Title = issue.Title,
                        Description = issue.Description,
                        State = issue.State.ToString(),
                        CreatedAt = issue.CreatedAt,
                        UpdatedAt = issue.UpdatedAt,
                        ClosedAt = issue.ClosedAt,
                        Author = new
                        {
                            Name = issue.Author?.Name,
                            Username = issue.Author?.Username,
                            Id = issue.Author?.Id
                        },
                        Assignees = issue.Assignees?.Select(a => new
                        {
                            Name = a.Name,
                            Username = a.Username,
                            Id = a.Id
                        }).ToArray(),
                        Labels = issue.Labels,
                        Milestone = issue.Milestone != null ? new
                        {
                            Id = issue.Milestone.Id,
                            Title = issue.Milestone.Title,
                            Description = issue.Milestone.Description,
                            State = issue.Milestone.State.ToString()
                        } : null,
                        WebUrl = issue.WebUrl,
                        UserNotesCount = issue.UserNotesCount,
                        DueDate = issue.DueDate,
                        Confidential = issue.Confidential,
                        Weight = issue.Weight,
                    },
                    ProjectId = projectId
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "GetIssue",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to get issue #{issueIid}: {ex.Message}"
                };
            }
        }

        [Description("Update an issue in a GitLab project. Parameters: projectId (string), issueIid (int), title (optional string), description (optional string), stateEvent (optional string - 'close' or 'reopen'), labels (optional string), assigneeIds (optional string - comma-separated IDs)")]
        private async Task<GitLabOutput> UpdateIssue(
    string projectId,
    int issueIid,
    string? title = null,
    string? description = null,
    string? stateEvent = null,
    string? labels = null,
    string? assigneeIds = null)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                var updateRequest = new UpdateIssueRequest();

                if (!string.IsNullOrEmpty(title))
                    updateRequest.Title = title;

                if (!string.IsNullOrEmpty(description))
                    updateRequest.Description = description;

                if (!string.IsNullOrEmpty(labels))
                    updateRequest.Labels = labels.Split(',').Select(l => l.Trim()).ToArray();

                if (!string.IsNullOrEmpty(assigneeIds))
                {
                    var ids = assigneeIds.Split(',').Select(id => int.Parse(id.Trim())).ToList();
                    updateRequest.Assignees = ids;
                }

                var issue = await gitLabClient.Issues.UpdateAsync(projectId, issueIid, updateRequest);

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "UpdateIssue",
                    Content = $"Successfully updated issue #{issue.Iid}",
                    Data = new
                    {
                        IssueId = issue.Id,
                        IssueIid = issue.Iid,
                        Title = issue.Title,
                        Description = issue.Description,
                        State = issue.State.ToString(),
                        UpdatedAt = issue.UpdatedAt,
                        Labels = issue.Labels,
                        Assignees = issue.Assignees?.Select(a => a.Name).ToArray()
                    },
                    ProjectId = projectId
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "UpdateIssue",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to update issue: {ex.Message}"
                };
            }
        }

        [Description("Add a new note (comment) to an issue. Parameters: projectId (string), issueIid (int), body (string - the comment text)")]
        private async Task<GitLabOutput> CreateIssueNote(string projectId, int issueIid, string body)
        {
            try
            {
                if (gitLabClient == null)
                    throw new InvalidOperationException("GitLab client not initialized");

                var note = await gitLabClient.Issues.CreateNoteAsync(projectId, issueIid, new CreateIssueNoteRequest(body));

                return new GitLabOutput
                {
                    Success = true,
                    Operation = "CreateIssueNote",
                    Content = $"Successfully added comment to issue #{issueIid}",
                    Data = new
                    {
                        NoteId = note.Id,
                        Body = note.Body,
                        Author = note.Author?.Name,
                        CreatedAt = note.CreatedAt,
                        UpdatedAt = note.UpdatedAt,
                        System = note.System,
                        NoteableId = note.NoteableId,
                        NoteableType = note.NoteableType
                    },
                    ProjectId = projectId
                };
            }
            catch (Exception ex)
            {
                return new GitLabOutput
                {
                    Success = false,
                    Operation = "CreateIssueNote",
                    ErrorMessage = ex.Message,
                    Content = $"Failed to add comment to issue: {ex.Message}"
                };
            }
        }

        protected override GitLabOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            var toolMethods = new[] {
                "CreateOrUpdateFile", "PushFiles", "SearchRepositories",
                "GetFileContents", "CreateIssue", "CreateMergeRequest",
                "GetIssue", "SearchCodeInRepository", "CreateBranch",
                "CreateIssueNote", "UpdateIssue", "GetDefaultBranch"
                };

            foreach (var method in toolMethods)
            {
                var toolResult = metadata.FindToolResult(method);
                if (!string.IsNullOrEmpty(toolResult))
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var result = JsonSerializer.Deserialize<GitLabOutput>(toolResult, options);
                        if (result != null)
                        {
                            result.Metadata = metadata;
                            return result;
                        }
                    }
                    catch
                    {
                        // Fallback if deserialization fails
                    }
                }
            }

            // Fallback to text output
            return new GitLabOutput
            {
                Success = !finalLLMAnswer.Contains("Error") && !finalLLMAnswer.Contains("Failed"),
                Content = finalLLMAnswer,
                Operation = "General",
                Metadata = metadata
            };
        }

        // Helper class for file operations
        private class FileItem
        {
            public string FilePath { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }
    }
}