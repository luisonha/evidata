namespace Evidata.Modules.Mcp.Domain;

/// <summary>Nivel de riesgo de la respuesta generada por el modelo.</summary>
public enum McpRiskLevel { Low, Medium, High }

/// <summary>Estado de la interacción MCP.</summary>
public enum McpInteractionStatus { Completed, Failed, ReviewRequested }
