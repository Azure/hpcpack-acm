namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class MultiTaskItem : TaskItem
    {
        private readonly TaskItem[] taskItems;
        public MultiTaskItem(params TaskItem[] items)
        {
            this.taskItems = items;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                foreach (var t in this.taskItems)
                {
                    t.Dispose();
                }
            }

            base.Dispose(isDisposing);
        }

        public TaskItem[] GetTaskItems() => this.taskItems;
        public override T GetMessage<T>() => typeof(T) == typeof(TaskItem[]) ? this.GetTaskItems() as T : default(T);
        public override Task FinishAsync(CancellationToken token) => Task.WhenAll(this.taskItems.Select(t => t.FinishAsync(token)));
        public override Task ReturnAsync(CancellationToken token) => Task.WhenAll(this.taskItems.Select(t => t.ReturnAsync(token)));
    }
}
