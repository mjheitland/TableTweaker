using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Host;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace TableTweaker
{
    internal class OpenDocumentViewModel : NotificationObject
    {
        private readonly string _workingDirectory;
        private readonly Dispatcher _dispatcher;

        private ExecutionHost _executionHost;
        private ObservableCollection<ResultObjectViewModel> _results;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private int _runToken;
        private Action<object> _executionHostOnDumped;
        private readonly object _resultsLock;
        private Platform _platform;

        public ObservableCollection<ResultObjectViewModel> Results
        {
            get { return _results; }
            private set { SetProperty(ref _results, value); }
        }

        public DocumentViewModel Document { get; }

        public OpenDocumentViewModel(MainViewModel mainViewModel, DocumentViewModel document)
        {
            Document = document;
            MainViewModel = mainViewModel;
            NuGet = new NuGetDocumentViewModel(mainViewModel.NuGet);
            _dispatcher = Dispatcher.CurrentDispatcher;

            var roslynHost = mainViewModel.RoslynHost;

            _workingDirectory = Directory.GetCurrentDirectory();

            Platform = Platform.X86;
            _executionHost = new ExecutionHost(GetHostExeName(), _workingDirectory,
                roslynHost.DefaultReferences.OfType<PortableExecutableReference>().Select(x => x.FilePath),
                roslynHost.DefaultImports, mainViewModel.NuGetProvider, mainViewModel.ChildProcessManager);
            _executionHost.ExecutionCompleted += OnExecutionCompleted;

            _resultsLock = new object();
            Results = new ObservableCollection<ResultObjectViewModel>();
            BindingOperations.EnableCollectionSynchronization(Results, _resultsLock);

            RunCommand = new DelegateCommand(Run, () => !IsRunning);
            RestartHostCommand = new DelegateCommand(RestartHost);
        }

        private string GetHostExeName()
        {
            switch (Platform)
            {
                case Platform.X86:
                    return "RoslynPad.Host32.exe";
                case Platform.X64:
                    return "RoslynPad.Host64.exe";
                default:
                    throw new ArgumentOutOfRangeException(nameof(Platform));
            }
        }

        public Platform Platform
        {
            get { return _platform; }
            set
            {
                if (SetProperty(ref _platform, value))
                {
                    if (_executionHost != null)
                    {
                        _executionHost.HostPath = GetHostExeName();
                        RestartHostCommand.Execute();
                    }
                }
            }
        }

        private void OnExecutionCompleted(int token)
        {
            if (token == _runToken)
            {
                SetIsRunning(false);
            }
        }

        private async Task RestartHost()
        {
            Reset();
            await _executionHost.ResetAsync().ConfigureAwait(false);
            SetIsRunning(false);
        }

        private void SetIsRunning(bool value)
        {
            _dispatcher.InvokeAsync(() => IsRunning = value);
        }

        public async Task Initialize(SourceTextContainer sourceTextContainer, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated)
        {
            var roslynHost = MainViewModel.RoslynHost;
            // ReSharper disable once AssignNullToNotNullAttribute
            DocumentId = roslynHost.AddDocument(sourceTextContainer, _workingDirectory, onDiagnosticsUpdated, onTextUpdated);
            await _executionHost.ResetAsync().ConfigureAwait(false);
        }

        public DocumentId DocumentId { get; private set; }

        public MainViewModel MainViewModel { get; }

        public NuGetDocumentViewModel NuGet { get; }

        public string Title => Document != null && !Document.IsAutoSaveOnly ? Document.Name : "New";

        public DelegateCommand RunCommand { get; }

        public DelegateCommand RestartHostCommand { get; }

        public bool IsRunning
        {
            get { return _isRunning; }
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    _dispatcher.InvokeAsync(() => RunCommand.RaiseCanExecuteChanged());
                }
            }
        }

        private async Task Run()
        {
            Reset();

            SetIsRunning(true);

            var token = Interlocked.Increment(ref _runToken);

            var results = new ObservableCollection<ResultObjectViewModel>();
            Results = results;

            var cancellationToken = _cts.Token;
            if (_executionHostOnDumped != null)
            {
                _executionHost.Dumped -= _executionHostOnDumped;
            }
            _executionHostOnDumped = o => AddResult(o, results, cancellationToken);
            _executionHost.Dumped += _executionHostOnDumped;
            try
            {
                var code = await MainViewModel.RoslynHost.GetDocument(DocumentId).GetTextAsync(cancellationToken).ConfigureAwait(true);
                await _executionHost.ExecuteAsync(code.ToString(), token).ConfigureAwait(true);
            }
            catch (CompilationErrorException ex)
            {
                lock (_resultsLock)
                {
                    foreach (var diagnostic in ex.Diagnostics)
                    {
                        results.Add(new ResultObjectViewModel(ResultObject.Create(diagnostic)));
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult(ex, results, cancellationToken);
            }
        }

        private void Reset()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
        }

        private void AddResult(object o, ObservableCollection<ResultObjectViewModel> results, CancellationToken cancellationToken)
        {
            _dispatcher.InvokeAsync(() =>
            {
                lock (_resultsLock)
                {
                    results.Add(new ResultObjectViewModel(o as ResultObject ?? ResultObject.Create(o)));
                }
            }, DispatcherPriority.SystemIdle, cancellationToken);
        }

        public void Close()
        {
            _executionHost?.Dispose();
            _executionHost = null;
        }
    }
}