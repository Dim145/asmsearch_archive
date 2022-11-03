using Microsoft.AspNetCore.Components;

namespace AnimeSearch.Site.Views.BlazorComponent;

public partial class Stepper
{
    private int CurrentStep { get; set; } = 0;

    [Parameter] public RenderFragment ChildContent { get; set; }

    [Parameter] public bool Linear { get; set; } = false;
    
    [Parameter] public Action OnFinish { get; set; }
    
    [Parameter] public bool IsVertical { get; set; }

    private List<Step> Steps { get; } = new();

    public void AddStep(Step s)
    {
        Steps.Add(s);

        InvokeAsync(StateHasChanged);
    }

    private void Next()
    {
        var step = Steps[CurrentStep];

        var message = step.IsValidStep?.Invoke();
        
        if (string.IsNullOrWhiteSpace(message) )
        {
            step.InError = false;
            step.ErrorMessage = string.Empty;
            CurrentStep = Math.Min(CurrentStep + 1, Steps.Count - 1);
        }
        else
        {
            step.InError = true;
            step.ErrorMessage = message;
        }
    }

    private void Precedent()
    {
        CurrentStep = Math.Max(CurrentStep - 1, 0);
    }

    private void SetStep(int step)
    {
        if (step > CurrentStep)
        {
            var cStep = Steps[CurrentStep];
            var message = cStep.IsValidStep?.Invoke();
            
            
            if (!string.IsNullOrWhiteSpace(message))
            {
                cStep.InError = true;
                cStep.ErrorMessage = message;
                return;
            }
        }

        if (!Linear && step > -1 && step < Steps.Count)
        {
            Steps[CurrentStep].InError = false;
            Steps[CurrentStep].ErrorMessage = string.Empty;
            CurrentStep = step;
        }
    }

    private void Finish()
    {
        foreach (var step in Steps)
        {
            var message = step.IsValidStep?.Invoke();

            step.InError = !string.IsNullOrWhiteSpace(message);
            step.ErrorMessage = message;
        }
        
        if(Steps.All(s => !s.InError))
            OnFinish?.Invoke();
    }
}