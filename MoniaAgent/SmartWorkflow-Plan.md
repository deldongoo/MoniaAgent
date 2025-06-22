# SmartWorkflow - Plan d'évolution

## État actuel (v1.0 - Fonctionnel)

### Ce qui fonctionne aujourd'hui

Le SmartWorkflow actuel permet une orchestration basique avec sélection automatique d'agent unique :

```csharp
var smartWorkflow = new SmartWorkflow(llm);
smartWorkflow.RegisterAgentType<TimeAgent>("Handles time-related queries and scheduling");
smartWorkflow.RegisterAgentType<FileReaderAgent>("Reads and processes files from the filesystem");

var result = await smartWorkflow.ExecuteWithPlanning("What time is it now?");
```

**Architecture actuelle :**
- `AgentRegistry` : Stockage simple (Type + description)
- `PlannerAgent` : Sélection d'un agent unique basée sur descriptions
- `SmartWorkflow` : Instanciation dynamique et exécution
- Agents autonomes : Utilisables indépendamment du SmartWorkflow

### Agents supportés

Les agents héritent de `TypedAgent<TInput, TOutput>` avec fallback `TextInput` pour compatibilité SmartWorkflow :

- **TimeAgent** : Requêtes temporelles
- **FileReaderAgent** : Lecture de fichiers 
- **McpDesktopCommanderAgent** : Commandes desktop via MCP
- Extensible pour nouveaux agents externes

## Limitations actuelles

### 1. Descriptions d'agents insuffisantes

**Problème :** Duplication et manque de richesse
```csharp
// Dans AgentConfig
Specialty = "Time and scheduling queries"
Keywords = ["time", "date", "when", "schedule", "timezone"]

// Dans Registry (redondant)
description = "Handles time-related queries and scheduling"
```

**Impact :** PlannerAgent ne voit que la description basique, pas les keywords ni capabilities détaillées.

### 2. Sélection d'agent simpliste

Le PlannerAgent actuel :
- Ne voit que nom + description courte
- Pas d'accès aux `Keywords`, `SupportedInputTypes`, capacités détaillées
- Sélection basée sur matching textuel basique
- Retourne seulement un nom d'agent (pas de plan structuré)

### 3. Pas d'orchestration multi-agents

**Limitation majeure :** Une tâche = un agent uniquement
- Pas de décomposition de tâches complexes
- Pas de pipeline d'agents
- Pas de gestion de dépendances

### 4. Contraintes techniques

- **Instanciation temporaire problématique** : Déclenche serveurs MCP
- **Agents doivent rester autonomes** : Utilisables hors SmartWorkflow
- **Framework simple** : Éviter over-engineering prématuré

## Vision future (v2.0 - SmartWorkflow complet)

### Orchestration multi-agents intelligente

```csharp
var workflow = new SmartWorkflow(llm)
    .RegisterAgentType<FileReaderAgent>()
    .RegisterAgentType<TimeAgent>()
    .RegisterAgentType<EmailAgent>();

// Tâche complexe nécessitant plusieurs agents
var result = await workflow.ExecuteWithPlanning(
    "Read config.json, check the current time, and send a status email with both informations"
);
```

**Résultat attendu :**
1. FileReaderAgent lit config.json
2. TimeAgent récupère l'heure actuelle  
3. EmailAgent envoie email avec config + heure

### Fonctionnalités cibles

- **Planification intelligente** : Décomposition automatique de tâches
- **Exécution parallèle/séquentielle** : Optimisation des performances
- **Gestion des dépendances** : Passage de données entre agents
- **Plans structurés** : JSON avec étapes, agents, conditions
- **Rollback intelligent** : Gestion d'erreurs avancée
- **Conditions d'exécution** : Étapes conditionnelles selon résultats

## Plan d'implémentation (4 phases)

### Phase 1 : Métadonnées enrichies des agents

**Objectif :** Donner plus d'informations au PlannerAgent sans casser l'autonomie

#### 1.1 Revoir le système de descriptions
- [ ] Éliminer duplication `Specialty` vs `description`
- [ ] Utiliser `AgentConfig` comme source unique de vérité
- [ ] Enrichir descriptions avec exemples concrets

#### 1.2 AgentRegistry enrichi
```csharp
public class AgentRegistration
{
    public Type AgentType { get; set; }
    public string Name { get; set; }
    public string Specialty { get; set; }
    public string[] Keywords { get; set; }
    public string[] Examples { get; set; }
    public Type[] SupportedInputTypes { get; set; }
    public Type ExpectedOutputType { get; set; }
}
```

#### 1.3 Extraction de métadonnées sans instanciation
```csharp
// Solution technique à définir :
// - Reflection sur Configure() sans constructeur complet
// - Attributs sur classes d'agents  
// - Registration explicite avec métadonnées
// - Interface IAgentMetadata statique
```

#### 1.4 PlannerAgent tool enrichi
```csharp
[Description("Get detailed agent capabilities with examples and supported operations")]
private string GetAgentCapabilities()
{
    // Retourne informations riches : Name, Specialty, Keywords, Examples, Types
    // Permet au LLM de faire des choix intelligents
}
```

### Phase 2 : PlannerAgent intelligent

**Objectif :** Générer des plans d'exécution structurés multi-agents

#### 2.1 Structure ExecutionPlan
```csharp
public class ExecutionPlan
{
    public string TaskDescription { get; set; }
    public List<ExecutionStep> Steps { get; set; }
    public Dictionary<string, object> GlobalContext { get; set; }
}

public class ExecutionStep  
{
    public string StepId { get; set; }
    public string AgentType { get; set; }
    public string TaskDescription { get; set; }
    public string[] DependsOn { get; set; }
    public bool CanRunInParallel { get; set; }
    public Dictionary<string, string> InputMappings { get; set; }
}
```

#### 2.2 PlannerAgent avancé
```csharp
public class SmartPlannerAgent : TypedAgent<TextInput, ExecutionPlanOutput>
{
    // Génère plans JSON structurés avec multiple agents
    // Analyse dépendances et parallélisme possible
    // Validation de faisabilité du plan
}
```

#### 2.3 Prompts de planification
- Instructions LLM pour analyser tâches complexes
- Guidelines pour décomposition en sous-tâches
- Format de sortie JSON structuré

### Phase 3 : Orchestrateur d'exécution

**Objectif :** Exécuter les plans multi-agents avec gestion de données

#### 3.1 Execution Engine
```csharp
public class PlanExecutor
{
    public async Task<WorkflowResult> ExecutePlan(ExecutionPlan plan, SmartWorkflow workflow)
    {
        // Exécution séquentielle/parallèle selon plan
        // Gestion du contexte global et passage de données
        // Instanciation dynamique des agents requis
    }
}
```

#### 3.2 Data Flow Management
- Système de variables globales
- Mapping des outputs vers inputs d'étapes suivantes
- Sérialisation/désérialisation automatique entre types

#### 3.3 Dependency Resolution
- Graphe de dépendances des étapes
- Détection de cycles
- Optimisation de l'ordre d'exécution

#### 3.4 Agent Lifecycle Management
- Pool d'agents réutilisables
- Instanciation à la demande
- Nettoyage des ressources

### Phase 4 : Fonctionnalités avancées

**Objectif :** Gestion d'erreurs, optimisations et fonctionnalités enterprise

#### 4.1 Error Handling & Rollback
```csharp
public class RollbackStrategy
{
    public async Task RollbackStep(ExecutionStep step, ExecutionContext context)
    {
        // Annulation des actions précédentes
        // Nettoyage des ressources
        // Notification des agents impactés
    }
}
```

#### 4.2 Conditional Execution
```csharp
public class ConditionalStep : ExecutionStep
{
    public string Condition { get; set; } // Expression à évaluer
    public ExecutionStep[] TrueBranch { get; set; }
    public ExecutionStep[] FalseBranch { get; set; }
}
```

#### 4.3 Performance Optimization
- Mise en cache des résultats d'agents
- Parallélisation intelligente
- Métriques de performance et monitoring

#### 4.4 Enterprise Features
- Timeout configurables par étape
- Retry policies sophistiquées
- Audit trail complet
- Support pour agents distribués

## Défis techniques à résoudre

### 1. Autonomie des agents
**Contrainte :** Agents doivent rester utilisables en dehors du SmartWorkflow
**Solution :** Éviter toute dépendance forte sur l'infrastructure SmartWorkflow

### 2. Extraction de métadonnées
**Problème :** Éviter instanciation temporaire (serveurs MCP)
**Options :**
- Reflection sur `Configure()` sans constructeur complet
- Attributs de classe pour métadonnées
- Interface `IAgentMetadata` statique
- Registration explicite avec métadonnées

### 3. Backward Compatibility
**Objectif :** Migration transparente de v1.0 vers v2.0
**Approche :** API additive, ancien code continue de fonctionner

### 4. Complexité vs Simplicité
**Équilibre :** Framework puissant mais simple à utiliser
**Principe :** Fonctionnalités avancées optionnelles, cas simples restent simples

## Exemples d'usage cibles

### Cas simple (doit rester simple)
```csharp
var workflow = new SmartWorkflow(llm);
workflow.RegisterAgentType<TimeAgent>();
var result = await workflow.ExecuteWithPlanning("What time is it?");
// Comportement identique à v1.0
```

### Cas complexe (nouvelle capacité)  
```csharp
var workflow = new SmartWorkflow(llm)
    .RegisterAgentType<FileReaderAgent>()
    .RegisterAgentType<DataAnalyzerAgent>() 
    .RegisterAgentType<ReportGeneratorAgent>()
    .RegisterAgentType<EmailAgent>();

var result = await workflow.ExecuteWithPlanning(
    "Analyze sales data from Q4-2024.csv, generate a summary report, and email it to the management team"
);

// Plan automatique :
// 1. FileReaderAgent lit Q4-2024.csv
// 2. DataAnalyzerAgent analyse les données  
// 3. ReportGeneratorAgent génère le rapport
// 4. EmailAgent envoie le rapport à l'équipe
```

### Cas avec conditions
```csharp
var result = await workflow.ExecuteWithPlanning(
    "Check system status, and if any issues are found, create a maintenance ticket and notify on-call engineer"
);

// Plan conditionnel :
// 1. SystemMonitorAgent vérifie le statut
// 2. Si problèmes détectés :
//    a. TicketAgent crée ticket de maintenance
//    b. NotificationAgent alerte l'ingénieur de garde
// 3. Sinon : Rapport "all good"
```

## Métriques de succès

### Phase 1
- [ ] Descriptions d'agents enrichies avec exemples
- [ ] PlannerAgent accède aux métadonnées complètes
- [ ] Sélection d'agents plus précise

### Phase 2  
- [ ] Plans JSON structurés générés automatiquement
- [ ] Support multi-agents dans les plans
- [ ] Validation de faisabilité des plans

### Phase 3
- [ ] Exécution de plans multi-agents fonctionnelle
- [ ] Passage de données entre agents
- [ ] Parallélisation automatique quand possible

### Phase 4
- [ ] Gestion d'erreurs robuste avec rollback
- [ ] Exécution conditionnelle
- [ ] Performance optimisée

## Timeline suggérée

- **Phase 1** : 1-2 semaines (métadonnées enrichies)
- **Phase 2** : 2-3 semaines (planification intelligente)  
- **Phase 3** : 3-4 semaines (orchestration multi-agents)
- **Phase 4** : 2-3 semaines (fonctionnalités avancées)

**Total estimé** : 8-12 semaines pour SmartWorkflow complet

---

*Document créé : 2024-06-22*  
*Statut : Plan initial - À réviser selon priorités business*