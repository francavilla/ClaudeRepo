using System;
using System.Collections.ObjectModel;
using System.Linq;
using BuildExe.Core.Model;
using BuildExe.Mvvm;

namespace BuildExe.ViewModels
{
    /// <summary>
    /// Un progetto eseguibile nell'elenco: selezione, target scelto, piano di build e stato dell'ultima esecuzione.
    /// </summary>
    public sealed class ProjectItemViewModel : ObservableObject
    {
        private readonly Action _changed;
        private bool _isSelected;
        private TargetFramework _selectedTargetFramework;
        private BuildPlan _plan;
        private string _runStatus;
        private string _outputDirectory;

        public ProjectItemViewModel(ProjectInfo info, Action changed)
        {
            Info = info;
            _changed = changed;
            TargetFrameworks = new ObservableCollection<TargetFramework>(info.TargetFrameworks);
            _selectedTargetFramework = info.DefaultTargetFramework;
        }

        public ProjectInfo Info { get; private set; }

        public string Name
        {
            get { return Info.Name; }
        }

        public ObservableCollection<TargetFramework> TargetFrameworks { get; private set; }

        public bool IsMultiTarget
        {
            get { return TargetFrameworks.Count > 1; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    _changed();
                }
            }
        }

        public TargetFramework SelectedTargetFramework
        {
            get { return _selectedTargetFramework; }
            set
            {
                // La ComboBox scrive null quando cambia il progetto corrente: va ignorato.
                if (value != null && SetProperty(ref _selectedTargetFramework, value))
                {
                    OnPropertyChanged("IsNetCoreTarget");
                    _changed();
                }
            }
        }

        /// <summary>Self-contained e single-file valgono solo per .NET Core / .NET 5+ SDK-style.</summary>
        public bool IsNetCoreTarget
        {
            get
            {
                return Info.IsSdkStyle
                    && SelectedTargetFramework != null
                    && SelectedTargetFramework.Family == FrameworkFamily.NetCore;
            }
        }

        public string TargetFrameworksText
        {
            get { return string.Join(", ", Info.TargetFrameworks.Select(t => t.DisplayName + " [" + t.Moniker + "]")); }
        }

        /// <summary>Cartella di output effettiva di questo progetto.</summary>
        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set { SetProperty(ref _outputDirectory, value); }
        }

        public BuildPlan Plan
        {
            get { return _plan; }
            set
            {
                if (SetProperty(ref _plan, value))
                {
                    OnPropertyChanged("ToolText");
                    OnPropertyChanged("CanBuild");
                    OnPropertyChanged("CommandPreview");
                    OnPropertyChanged("Status");
                }
            }
        }

        public bool CanBuild
        {
            get { return Plan != null && Plan.CanBuild; }
        }

        public string ToolText
        {
            get
            {
                if (Plan == null)
                {
                    return string.Empty;
                }

                switch (Plan.Tool)
                {
                    case BuildTool.DotNetCli:
                        return "dotnet publish";
                    case BuildTool.MsBuild:
                        return "MSBuild";
                    default:
                        return "—";
                }
            }
        }

        public string CommandPreview
        {
            get { return CanBuild ? Plan.CommandLineText : string.Empty; }
        }

        /// <summary>Esito dell'ultima build; null = non ancora eseguita.</summary>
        public string RunStatus
        {
            get { return _runStatus; }
            set
            {
                if (SetProperty(ref _runStatus, value))
                {
                    OnPropertyChanged("Status");
                }
            }
        }

        public string Status
        {
            get
            {
                if (RunStatus != null)
                {
                    return RunStatus;
                }

                if (Plan == null)
                {
                    return string.Empty;
                }

                return Plan.CanBuild ? "pronto" : "✖ " + Plan.Errors.FirstOrDefault();
            }
        }
    }
}
