 #nullable disable
  namespace TidyData
{
    public class AggregateCommand<TDataModel> : ICommand<TDataModel>
    {
        private readonly List<ICommand<TDataModel>> _commandSequence;

        public AggregateCommand(params ICommand<TDataModel>[] commandSequence)
            : this(commandSequence.AsEnumerable())
        {
        }

        public AggregateCommand(IEnumerable<ICommand<TDataModel>> commandSequence)
        {
            this._commandSequence = commandSequence.ToList();
        }

        public void Execute(TDataModel model, CollectionWrapperFactory factory)
        {
            foreach (ICommand<TDataModel> command in this._commandSequence)
                command.Execute(model, factory);
        }

        public bool HasEmptyCommandList()
        {
            return this._commandSequence.Count == 0;
        }
    }
}
