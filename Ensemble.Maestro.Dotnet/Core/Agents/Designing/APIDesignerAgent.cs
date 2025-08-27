using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Designing;

/// <summary>
/// API Designer agent responsible for designing RESTful APIs and service interfaces
/// </summary>
public class APIDesignerAgent : BaseDesignerAgent
{
    public APIDesignerAgent(
        ILogger<BaseAgent> logger, 
        ILLMService llmService, 
        IDesignerOutputStorageService designerOutputStorageService) 
        : base(logger, llmService, designerOutputStorageService) { }
    
    public override string AgentType => "APIDesigner";
    public override string AgentName => "API Designer";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Designing API specifications for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are an expert API designer specializing in RESTful API design, OpenAPI specifications, and modern web service architectures.

Your role is to create comprehensive API specifications and design documentation for a {context.TargetLanguage ?? "modern"} application targeting {context.DeploymentTarget ?? "cloud"} deployment.

Include in your response:
• Complete REST API design with resource modeling
• OpenAPI/Swagger specification structure  
• Authentication and authorization patterns
• Request/response schemas and validation rules
• Error handling and status code strategies
• API versioning and backward compatibility
• Rate limiting and throttling strategies
• Caching strategies and performance optimization
• Security best practices (OWASP API guidelines)
• Testing and documentation strategies

Provide specific, implementable API design formatted in clear markdown with code examples.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        
        if (!result.Success)
            return result;
        
        // Add API design-specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "api_specification.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/design/api_specification.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "openapi.yaml",
                Type = "yaml",
                Content = GenerateOpenAPISpec(context),
                Path = "/design/openapi.yaml",
                Size = 2000
            },
            new AgentArtifact
            {
                Name = "api_client.ts",
                Type = "typescript",
                Content = GenerateAPIClient(context),
                Path = "/design/api_client.ts",
                Size = 1500
            }
        };
        
        return result;
    }
    
    private string GenerateOpenAPISpec(AgentExecutionContext context)
    {
        var language = context.TargetLanguage ?? "Generic";
        
        return $@"openapi: 3.0.3
info:
  title: {language} Project API
  description: Comprehensive API specification for project management system
  version: 1.0.0
  contact:
    name: API Support
    email: api-support@example.com
  license:
    name: MIT
    url: https://opensource.org/licenses/MIT

servers:
  - url: https://api.example.com/v1
    description: Production server
  - url: https://staging-api.example.com/v1
    description: Staging server

paths:
  /projects:
    get:
      summary: List all projects
      operationId: listProjects
      tags:
        - Projects
      parameters:
        - name: limit
          in: query
          schema:
            type: integer
            minimum: 1
            maximum: 100
            default: 10
        - name: offset
          in: query
          schema:
            type: integer
            minimum: 0
            default: 0
      responses:
        '200':
          description: Successful response
          content:
            application/json:
              schema:
                type: object
                properties:
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/Project'
                  meta:
                    $ref: '#/components/schemas/PaginationMeta'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '500':
          $ref: '#/components/responses/InternalError'
    post:
      summary: Create new project
      operationId: createProject
      tags:
        - Projects
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateProjectRequest'
      responses:
        '201':
          description: Project created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Project'
        '400':
          $ref: '#/components/responses/BadRequest'
        '401':
          $ref: '#/components/responses/Unauthorized'

components:
  schemas:
    Project:
      type: object
      required:
        - id
        - name
        - status
        - createdAt
      properties:
        id:
          type: string
          format: uuid
        name:
          type: string
          minLength: 1
          maxLength: 255
        description:
          type: string
          maxLength: 1000
        status:
          type: string
          enum: [draft, active, completed, archived]
        targetLanguage:
          type: string
          nullable: true
        deploymentTarget:
          type: string
          nullable: true
        createdAt:
          type: string
          format: date-time
        updatedAt:
          type: string
          format: date-time

    CreateProjectRequest:
      type: object
      required:
        - name
      properties:
        name:
          type: string
          minLength: 1
          maxLength: 255
        description:
          type: string
          maxLength: 1000
        targetLanguage:
          type: string
        deploymentTarget:
          type: string

    PaginationMeta:
      type: object
      properties:
        total:
          type: integer
        limit:
          type: integer
        offset:
          type: integer
        hasNext:
          type: boolean
        hasPrevious:
          type: boolean

    Error:
      type: object
      required:
        - code
        - message
      properties:
        code:
          type: string
        message:
          type: string
        details:
          type: object

  responses:
    BadRequest:
      description: Bad request
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Error'
    Unauthorized:
      description: Unauthorized
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Error'
    InternalError:
      description: Internal server error
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Error'

  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT

security:
  - BearerAuth: []";
    }
    
    private string GenerateAPIClient(AgentExecutionContext context)
    {
        return @"// TypeScript API Client
export interface Project {
  id: string;
  name: string;
  description?: string;
  status: 'draft' | 'active' | 'completed' | 'archived';
  targetLanguage?: string;
  deploymentTarget?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
  targetLanguage?: string;
  deploymentTarget?: string;
}

export interface APIResponse<T> {
  data: T;
  meta?: {
    total: number;
    limit: number;
    offset: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
}

export interface APIError {
  code: string;
  message: string;
  details?: any;
}

export class APIClient {
  private baseUrl: string;
  private authToken?: string;

  constructor(baseUrl: string, authToken?: string) {
    this.baseUrl = baseUrl;
    this.authToken = authToken;
  }

  async listProjects(limit = 10, offset = 0): Promise<APIResponse<Project[]>> {
    const url = new URL('/projects', this.baseUrl);
    url.searchParams.set('limit', limit.toString());
    url.searchParams.set('offset', offset.toString());

    const response = await this.request('GET', url.toString());
    return response.json();
  }

  async createProject(project: CreateProjectRequest): Promise<Project> {
    const response = await this.request('POST', '/projects', project);
    return response.json();
  }

  private async request(method: string, path: string, body?: any): Promise<Response> {
    const url = path.startsWith('http') ? path : `${this.baseUrl}${path}`;
    
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    if (this.authToken) {
      headers.Authorization = `Bearer ${this.authToken}`;
    }

    const response = await fetch(url, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    if (!response.ok) {
      const error: APIError = await response.json();
      throw new Error(`API Error: ${error.message} (${error.code})`);
    }

    return response;
  }
}";
    }
    
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        // API design typically takes 3-5 minutes
        var baseTime = 180; // 3 minutes base
        var complexity = (context.InputPrompt?.Length ?? 0) / 300;
        return baseTime + (complexity * 25);
    }
}