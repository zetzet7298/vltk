using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public sealed class HongbaoOpenedItemRef
    {
        public HongbaoOpenedItemRef(int genre, int detail, int particular, int count = 1)
        {
            Genre = genre;
            Detail = detail;
            Particular = particular;
            Count = count <= 0 ? 1 : count;
        }

        public int Genre { get; }
        public int Detail { get; }
        public int Particular { get; }
        public int Count { get; }
    }

    public sealed class HongbaoRuntimeOperation
    {
        public string ApiName;
        public readonly List<int> Args = new List<int>();
        public string Message;
    }

    public interface IHongbaoRuntimeHost
    {
        List<HongbaoRuntimeOperation> CapturedOperations { get; }
        void ConsumeOpenedItem(int genre, int detail, int particular, int count);
        void AddItem(int genre, int detail, int particular, int level, int serise, int luck, int[] parameters);
        void AddGoldItem(int firstArg, int goldItemId);
        void Msg2Player(string message);
        void AddGlobalNews(string message);
        void WriteLog(string message);
        void Talk(string message);
    }

    public sealed class CapturingHongbaoRuntimeHost : IHongbaoRuntimeHost
    {
        private readonly List<HongbaoRuntimeOperation> _capturedOperations = new List<HongbaoRuntimeOperation>();
        public List<HongbaoRuntimeOperation> CapturedOperations => _capturedOperations;

        public void ConsumeOpenedItem(int genre, int detail, int particular, int count)
            => Add("ConsumeOpenedItem", null, genre, detail, particular, count);

        public void AddItem(int genre, int detail, int particular, int level, int serise, int luck, int[] parameters)
        {
            var op = Add("AddItem", null, genre, detail, particular, level, serise, luck);
            if (parameters != null)
                op.Args.AddRange(parameters);
        }

        public void AddGoldItem(int firstArg, int goldItemId)
            => Add("AddGoldItem", null, firstArg, goldItemId);

        public void Msg2Player(string message)
            => Add("Msg2Player", message);

        public void AddGlobalNews(string message)
            => Add("AddGlobalNews", message);

        public void WriteLog(string message)
            => Add("WriteLog", message);

        public void Talk(string message)
            => Add("Talk", message);

        private HongbaoRuntimeOperation Add(string apiName, string message, params int[] args)
        {
            var op = new HongbaoRuntimeOperation { ApiName = apiName, Message = message };
            if (args != null) op.Args.AddRange(args);
            _capturedOperations.Add(op);
            return op;
        }
    }
}
