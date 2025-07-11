using System;
using System.Linq;
using System.Reflection;
using MoniaAgent.Orchestration.Dynamic;
using Agent = MoniaAgent.Agent.Agent;

namespace MoniaAgent.Agent
{
    /// <summary>
    /// Helper class to extract agent metadata without instantiation
    /// </summary>
    internal static class AgentMetadataExtractor
    {
        /// <summary>
        /// Extract metadata from an agent type without instantiating it
        /// </summary>
        public static AgentRegistration? ExtractMetadata(Type agentType)
        {
            if (!IsAgentType(agentType))
                return null;

            var registration = new AgentRegistration
            {
                AgentType = agentType,
                Name = agentType.Name // Default name
            };

            // Extract metadata from attribute
            var metadataAttribute = agentType.GetCustomAttribute<AgentMetadataAttribute>();
            if (metadataAttribute != null)
            {
                registration.Name = metadataAttribute.Name;
                registration.Specialty = metadataAttribute.Specialty;
                registration.Keywords = metadataAttribute.Keywords ?? Array.Empty<string>();
                registration.Goal = metadataAttribute.Goal;
            }

            // Extract generic type parameters for input/output types
            var baseType = agentType.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(TypedAgent<,>))
                {
                    var genericArgs = baseType.GetGenericArguments();
                    registration.SupportedInputTypes = new[] { genericArgs[0] };
                    registration.ExpectedOutputType = genericArgs[1];
                    break;
                }
                baseType = baseType.BaseType;
            }

            // Try to extract tool names (limited without instantiation)
            registration.ToolNames = ExtractToolNames(agentType);

            return registration;
        }

        private static bool IsAgentType(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(Agent).IsAssignableFrom(type);
        }

        private static string[] ExtractToolNames(Type agentType)
        {
            // This is limited - we can only get method names that might be tools
            // Full tool information requires instantiation
            var toolMethods = agentType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>() != null)
                .Select(m => m.Name)
                .ToArray();

            return toolMethods;
        }
    }
}