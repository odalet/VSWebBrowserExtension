using System;
using System.Reflection;
using System.Windows.Input;

namespace MiniBrowser.Utils
{
    public sealed class RelayCommand : ICommand
    {
        private sealed class WeakAction
        {
            private Action staticAction;

            public WeakAction(Action action, bool keepTargetAlive = false) : this(action?.Target, action, keepTargetAlive) { }
            public WeakAction(object target, Action action, bool keepTargetAlive = false)
            {
                if (action.Method.IsStatic)
                {
                    staticAction = action;
                    if (target != null)
                        Reference = new WeakReference(target);
                }
                else
                {
                    Method = action.Method;
                    ActionReference = new WeakReference(action.Target);
                    LiveReference = keepTargetAlive ? action.Target : null;
                    Reference = new WeakReference(target);
                }
            }

            public string MethodName => staticAction == null ? Method.Name : staticAction.Method.Name;
            public object Target => Reference?.Target;

            public bool IsStatic => staticAction != null;
            public bool IsAlive
            {
                get
                {
                    if (staticAction == null && Reference == null && LiveReference == null)
                        return false;

                    if (staticAction != null)
                        return Reference == null || Reference.IsAlive;

                    if (LiveReference != null)
                        return true;

                    return Reference != null && Reference.IsAlive;
                }
            }

            private MethodInfo Method { get; set; }
            private WeakReference ActionReference { get; set; }
            private object LiveReference { get; set; }
            private WeakReference Reference { get; set; }
            private object ActionTarget => LiveReference ?? (ActionReference?.Target);

            public void Execute()
            {
                if (staticAction != null)
                {
                    staticAction();
                    return;
                }

                var actionTarget = ActionTarget;
                if (IsAlive && Method != null && (LiveReference != null || ActionReference != null) && actionTarget != null)
                    _ = Method.Invoke(actionTarget, null);
            }

            public void MarkForDeletion()
            {
                Reference = null;
                ActionReference = null;
                LiveReference = null;
                Method = null;
                staticAction = null;
            }
        }

        private sealed class WeakFunc<TResult>
        {
            private Func<TResult> staticFunc;

            public bool IsStatic => staticFunc != null;
            public string MethodName => staticFunc == null ? Method.Name : staticFunc.Method.Name;
            public object Target => Reference?.Target;
            public bool IsAlive
            {
                get
                {
                    if (staticFunc == null && Reference == null && LiveReference == null)
                        return false;

                    if (staticFunc != null)
                        return Reference == null || Reference.IsAlive;

                    if (LiveReference != null)
                        return true;

                    return Reference != null && Reference.IsAlive;
                }
            }

            private MethodInfo Method { get; set; }
            private WeakReference FuncReference { get; set; }
            private object LiveReference { get; set; }
            private WeakReference Reference { get; set; }
            private object FuncTarget => LiveReference ?? (FuncReference?.Target);

            public WeakFunc(Func<TResult> func, bool keepTargetAlive = false) : this(func?.Target, func, keepTargetAlive) { }
            public WeakFunc(object target, Func<TResult> func, bool keepTargetAlive = false)
            {
                if (func.Method.IsStatic)
                {
                    staticFunc = func;
                    if (target != null)
                        Reference = new WeakReference(target);
                }
                else
                {
                    Method = func.Method;
                    FuncReference = new WeakReference(func.Target);
                    LiveReference = keepTargetAlive ? func.Target : null;
                    Reference = new WeakReference(target);
                }
            }

            public TResult Execute()
            {
                if (staticFunc != null)
                    return staticFunc();

                object funcTarget = FuncTarget;
                return !IsAlive || Method == null || LiveReference == null && FuncReference == null || funcTarget == null
                    ? default
                    : (TResult)Method.Invoke(funcTarget, null);
            }

            public void MarkForDeletion()
            {
                Reference = null;
                FuncReference = null;
                LiveReference = null;
                Method = null;
                staticFunc = null;
            }
        }

        private readonly WeakAction executeAction;
        private readonly WeakFunc<bool> canExecuteFunc;
        private EventHandler requerySuggestedLocalEventHandler;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (canExecuteFunc == null) return;

                EventHandler handler2;
                var canExecuteChanged = requerySuggestedLocalEventHandler;

                do
                {
                    handler2 = canExecuteChanged;
                    var handler3 = (EventHandler)Delegate.Combine(handler2, value);
                    canExecuteChanged = System.Threading.Interlocked.CompareExchange(ref requerySuggestedLocalEventHandler, handler3, handler2);
                }
                while (canExecuteChanged != handler2);

                CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (canExecuteFunc == null) return;

                EventHandler handler2;
                var canExecuteChanged = requerySuggestedLocalEventHandler;

                do
                {
                    handler2 = canExecuteChanged;
                    var handler3 = (EventHandler)Delegate.Remove(handler2, value);
                    canExecuteChanged = System.Threading.Interlocked.CompareExchange(ref requerySuggestedLocalEventHandler, handler3, handler2);
                }
                while (canExecuteChanged != handler2);

                CommandManager.RequerySuggested -= value;
            }
        }

        public RelayCommand(Action execute, bool keepTargetAlive = false) : this(execute, null, keepTargetAlive) { }
        public RelayCommand(Action execute, Func<bool> canExecute, bool keepTargetAlive = false)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            executeAction = new WeakAction(execute, keepTargetAlive);
            if (canExecute != null)
                canExecuteFunc = new WeakFunc<bool>(canExecute, keepTargetAlive);
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

        public bool CanExecute(object parameter)
        {
            if (canExecuteFunc != null)
                return (canExecuteFunc.IsStatic || canExecuteFunc.IsAlive) && canExecuteFunc.Execute();
            return true;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter) && executeAction != null && (executeAction.IsStatic || executeAction.IsAlive))
                executeAction.Execute();
        }
    }
}
