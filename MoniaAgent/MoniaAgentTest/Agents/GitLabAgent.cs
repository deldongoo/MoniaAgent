using GitLabApiClient;
using GitLabApiClient.Models.Commits.Requests;
using GitLabApiClient.Models.Commits.Requests.CreateCommitRequest;
using GitLabApiClient.Models.Issues.Requests;
using GitLabApiClient.Models.MergeRequests.Requests;
using GitLabApiClient.Models.Projects.Requests;
using MoniaAgent.Configuration;
using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
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
                CreateMergeRequest
            }
        };

        [Description("Create or update a single file in a GitLab project. Parameters: projectId (string), filePath (string), content (string), commitMessage (string), branch (string), previousPath (optional string)")]
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

                if (previousPath != null)
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
                    // Create/Update operation
                    actions.Add(new CreateCommitRequestAction(CreateCommitRequestActionType.Create, filePath)
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
                    Content = $"File {filePath} successfully {(previousPath != null ? "moved and updated" : "created/updated")} in project {projectId}",
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

        protected override GitLabOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            // Check for tool results in metadata
            var toolMethods = new[] {
                "CreateOrUpdateFile", "PushFiles", "SearchRepositories",
                "GetFileContents", "CreateIssue", "CreateMergeRequest"
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