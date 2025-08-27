using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Designing;

/// <summary>
/// UI Designer agent responsible for creating user interface designs and user experience specifications
/// </summary>
public class UIDesignerAgent : BaseDesignerAgent
{
    public UIDesignerAgent(
        ILogger<BaseAgent> logger, 
        ILLMService llmService, 
        IDesignerOutputStorageService designerOutputStorageService) 
        : base(logger, llmService, designerOutputStorageService) { }
    
    public override string AgentType => "UIDesigner";
    public override string AgentName => "UI/UX Designer";
    public override string Priority => "Medium";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating UI/UX design specifications for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a senior UI/UX designer specializing in modern web interfaces and user experience optimization.

Your role is to create comprehensive UI/UX design specifications based on the user's requirements.

For the target language: {context.TargetLanguage ?? "Modern Web Application"}
For deployment target: {context.DeploymentTarget ?? "Multi-platform"}

Include in your response:
• Complete design system (colors, typography, spacing)
• Component library specifications
• Responsive design considerations
• Accessibility standards (WCAG 2.1)
• User experience patterns and interaction flows
• Performance considerations for UI implementation
• Detailed wireframe descriptions

Provide specific, actionable design specifications formatted in clear markdown with proper headings and structure.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        if (!result.Success) return result;
        
        // Add UI designer specific artifacts
        var wireframesContent = GenerateWireframes(context);
        var styleGuideContent = GenerateStyleGuide();
        
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "ui_design_system.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/design/ui_design_system.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "wireframes.html",
                Type = "html",
                Content = wireframesContent,
                Path = "/design/wireframes.html",
                Size = wireframesContent.Length
            },
            new AgentArtifact
            {
                Name = "style_guide.css",
                Type = "css",
                Content = styleGuideContent,
                Path = "/design/style_guide.css",
                Size = styleGuideContent.Length
            }
        };
        
        return result;
    }
    
    private string GenerateUIDesignOutput(AgentExecutionContext context)
    {
        var language = context.TargetLanguage ?? "Web-based";
        var deployment = context.DeploymentTarget ?? "Multi-platform";
        
        return $@"# UI/UX Design Specification

## Design Philosophy
Creating an intuitive, accessible, and performant user interface for {language} application targeting {deployment} deployment with modern design principles.

## Design System

### Color Palette
- **Primary**: #0066CC (Professional Blue)
- **Secondary**: #28A745 (Success Green)
- **Accent**: #FF6B35 (Energetic Orange)
- **Neutral**: #6C757D (Balanced Gray)
- **Background**: #F8F9FA (Light Gray)
- **Text**: #212529 (Dark Gray)

### Typography
- **Primary Font**: Inter (System UI fallback)
- **Monospace**: 'JetBrains Mono', Consolas, monospace
- **Heading Scale**: 2.5rem → 2rem → 1.5rem → 1.25rem
- **Body Text**: 1rem (16px) with 1.5 line height
- **Small Text**: 0.875rem (14px) for captions

### Spacing System
- **Base Unit**: 8px
- **Scales**: 4px, 8px, 16px, 24px, 32px, 48px, 64px
- **Grid**: 12-column responsive grid system
- **Breakpoints**: 576px, 768px, 992px, 1200px, 1400px

## Component Library

### Navigation
- **Header**: Fixed navigation with logo, main menu, user actions
- **Sidebar**: Collapsible navigation for admin sections
- **Breadcrumbs**: Context-aware navigation trail
- **Pagination**: Standard and infinite scroll variants

### Forms
- **Input Fields**: Consistent styling with validation states
- **Buttons**: Primary, secondary, tertiary, and icon variants
- **Dropdowns**: Single/multi-select with search capability
- **Checkboxes/Radio**: Custom styled form controls

### Data Display
- **Tables**: Sortable, filterable, responsive data tables
- **Cards**: Content containers with consistent padding
- **Lists**: Simple and complex list item layouts
- **Charts**: Integration-ready chart containers

### Feedback
- **Alerts**: Success, warning, error, info notifications
- **Modals**: Centered overlays for important actions
- **Tooltips**: Contextual help and information
- **Progress**: Linear and circular progress indicators

## User Experience Patterns

### Navigation Patterns
- **Dashboard First**: Landing on overview/status page
- **Progressive Disclosure**: Show complexity gradually
- **Consistent Actions**: Same actions in same locations
- **Keyboard Shortcuts**: Power user accessibility

### Interaction Patterns
- **Immediate Feedback**: Visual response to all actions
- **Optimistic Updates**: Assume success, handle failures gracefully
- **Bulk Operations**: Select multiple items for batch actions
- **Drag & Drop**: Intuitive file uploads and reordering

### Information Architecture
- **Hierarchy**: Clear visual hierarchy with consistent patterns
- **Grouping**: Related information grouped logically
- **Scanning**: F-pattern layout for easy content scanning
- **Search**: Global search with filters and suggestions

## Responsive Design

### Mobile First
- **Touch Targets**: Minimum 44px touch target size
- **Navigation**: Hamburger menu with clear hierarchy
- **Content**: Stacked layout with appropriate spacing
- **Performance**: Optimized images and minimal resources

### Tablet Optimization
- **Grid Layout**: 2-column layout where appropriate
- **Mixed Interaction**: Both touch and precision inputs
- **Orientation**: Adapt to portrait/landscape changes
- **Multitasking**: Consider split-screen scenarios

### Desktop Enhancement
- **Multi-column**: Utilize available screen real estate
- **Hover States**: Rich hover interactions and tooltips
- **Keyboard Navigation**: Full keyboard accessibility
- **Multiple Windows**: Support for multi-window workflows

## Accessibility Standards

### WCAG 2.1 Compliance
- **AA Standard**: Minimum contrast ratios and font sizes
- **Screen Readers**: Proper ARIA labels and semantic HTML
- **Keyboard Navigation**: Tab order and focus management
- **Color Independence**: Information not conveyed by color alone

### Inclusive Design
- **Internationalization**: RTL language support preparation
- **Reduced Motion**: Respect prefers-reduced-motion
- **High Contrast**: Support for high contrast mode
- **Font Scaling**: Graceful handling of enlarged text

## Performance Considerations

### Loading Performance
- **Critical CSS**: Above-fold styles inlined
- **Font Loading**: Optimized web font loading strategy
- **Image Optimization**: Responsive images with proper formats
- **Code Splitting**: Route-based component lazy loading

### Runtime Performance
- **Smooth Animations**: 60fps animations using transforms
- **Efficient Rendering**: Virtual scrolling for large lists
- **Memory Management**: Component cleanup and state management
- **Bundle Size**: Tree shaking and code optimization

UI/UX specifications ready for frontend implementation.";
    }
    
    private string GenerateWireframes(AgentExecutionContext context)
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>UI Wireframes</title>
    <style>
        .wireframe { border: 2px solid #ccc; margin: 20px; padding: 20px; background: #f9f9f9; }
        .header { height: 60px; background: #ddd; margin-bottom: 20px; }
        .sidebar { width: 200px; height: 400px; background: #eee; float: left; }
        .main { margin-left: 220px; height: 400px; background: #f5f5f5; }
        .card { height: 120px; background: white; margin: 10px; border: 1px solid #ddd; }
    </style>
</head>
<body>
    <div class=""wireframe"">
        <h3>Dashboard Layout</h3>
        <div class=""header""></div>
        <div class=""sidebar""></div>
        <div class=""main"">
            <div class=""card""></div>
            <div class=""card""></div>
            <div class=""card""></div>
        </div>
        <div style=""clear: both;""></div>
    </div>
    
    <div class=""wireframe"">
        <h3>Modal Dialog</h3>
        <div style=""width: 400px; height: 300px; background: white; border: 2px solid #333; margin: 0 auto;"">
            <div style=""height: 50px; background: #f0f0f0; border-bottom: 1px solid #ccc;""></div>
            <div style=""padding: 20px; height: 200px;""></div>
            <div style=""height: 50px; background: #f0f0f0; border-top: 1px solid #ccc;""></div>
        </div>
    </div>
</body>
</html>";
    }
    
    private string GenerateStyleGuide()
    {
        return @":root {
  /* Colors */
  --color-primary: #0066CC;
  --color-secondary: #28A745;
  --color-accent: #FF6B35;
  --color-neutral: #6C757D;
  --color-background: #F8F9FA;
  --color-text: #212529;
  --color-text-muted: #6C757D;
  --color-border: #DEE2E6;
  
  /* Typography */
  --font-family-primary: 'Inter', system-ui, sans-serif;
  --font-family-mono: 'JetBrains Mono', 'Consolas', monospace;
  --font-size-xs: 0.75rem;
  --font-size-sm: 0.875rem;
  --font-size-base: 1rem;
  --font-size-lg: 1.125rem;
  --font-size-xl: 1.25rem;
  --font-size-2xl: 1.5rem;
  --font-size-3xl: 2rem;
  --font-size-4xl: 2.5rem;
  
  /* Spacing */
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  --spacing-lg: 1.5rem;
  --spacing-xl: 2rem;
  --spacing-2xl: 3rem;
  --spacing-3xl: 4rem;
  
  /* Shadows */
  --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  --shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
  
  /* Borders */
  --border-radius-sm: 0.25rem;
  --border-radius-md: 0.375rem;
  --border-radius-lg: 0.5rem;
  --border-radius-xl: 0.75rem;
  --border-radius-full: 9999px;
}

/* Base styles */
body {
  font-family: var(--font-family-primary);
  font-size: var(--font-size-base);
  line-height: 1.5;
  color: var(--color-text);
  background-color: var(--color-background);
}

/* Button styles */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem 1rem;
  font-size: var(--font-size-base);
  font-weight: 500;
  border: 1px solid transparent;
  border-radius: var(--border-radius-md);
  cursor: pointer;
  transition: all 0.2s ease-in-out;
}

.btn-primary {
  background-color: var(--color-primary);
  color: white;
}

.btn-primary:hover {
  background-color: color-mix(in srgb, var(--color-primary) 90%, black);
}

/* Card styles */
.card {
  background: white;
  border: 1px solid var(--color-border);
  border-radius: var(--border-radius-lg);
  box-shadow: var(--shadow-sm);
  padding: var(--spacing-lg);
}

/* Form styles */
.form-control {
  display: block;
  width: 100%;
  padding: 0.5rem 0.75rem;
  font-size: var(--font-size-base);
  border: 1px solid var(--color-border);
  border-radius: var(--border-radius-md);
  transition: border-color 0.2s ease-in-out;
}

.form-control:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px rgba(0, 102, 204, 0.2);
}";
    }
    
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        // UI design work typically takes 2-4 minutes
        var baseTime = 160; // 2.7 minutes base
        var complexity = (context.InputPrompt?.Length ?? 0) / 300;
        return baseTime + (complexity * 20);
    }
}