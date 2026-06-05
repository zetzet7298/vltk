// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: Flip Card (Lật Thẻ / Bingo)
// Bảng UI cho trò chơi lật thẻ, ghép cặp, hoàn thành bingo.
// Vietnamese: "Lật Thẻ", "Bingo", "Đã lật", "Đã khớp", "Hoàn thành", "Chơi lại".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct FlipCardPanelRow
    {
        public readonly int cardId;
        public readonly string name;
        public readonly string iconPath;
        public readonly bool isFlipped;
        public readonly bool isMatched;
        public readonly int posX;
        public readonly int posY;
        public readonly int gridIndex;

        public FlipCardPanelRow(int cardId, string name, string iconPath, bool isFlipped, bool isMatched, int posX, int posY, int gridIndex)
        {
            this.cardId = cardId;
            this.name = name ?? string.Empty;
            this.iconPath = iconPath ?? string.Empty;
            this.isFlipped = isFlipped;
            this.isMatched = isMatched;
            this.posX = posX;
            this.posY = posY;
            this.gridIndex = gridIndex;
        }
    }

    public sealed class FlipCardPanelSnapshot
    {
        public int playerId;
        public int gameId;
        public int totalCards;
        public int flippedCount;
        public int matchedPairs;
        public bool isComplete;
        public IReadOnlyList<FlipCardPanelRow> rows;
    }

    public static class FlipCardPanelService
    {
        public const string LabelFlipCard = "Lật Thẻ";
        public const string LabelBingo = "Bingo";
        public const string LabelFlipped = "Đã lật";
        public const string LabelMatched = "Đã khớp";
        public const string LabelComplete = "Hoàn thành";
        public const string LabelRestart = "Chơi lại";

        public static FlipCardPanelSnapshot BuildSnapshot(FlipCardService service, int playerId)
        {
            var snapshot = new FlipCardPanelSnapshot
            {
                playerId = playerId,
                gameId = 0,
                totalCards = 0,
                flippedCount = 0,
                matchedPairs = 0,
                isComplete = false,
                rows = Array.Empty<FlipCardPanelRow>()
            };
            if (service == null) return snapshot;
            var all = service.GetAll();
            var rows = new List<FlipCardPanelRow>();
            int idx = 0;
            foreach (var card in all)
            {
                if (card == null) continue;
                int col = idx % 4;
                int row = idx / 4;
                rows.Add(new FlipCardPanelRow(
                    card.cardId, card.nameRaw, card.iconPath, false, false, col, row, idx));
                idx++;
            }
            snapshot.totalCards = rows.Count;
            snapshot.rows = rows;
            return snapshot;
        }

        public static FlipCardPanelRow? GetCard(int cardId)
        {
            if (cardId <= 0) return null;
            return new FlipCardPanelRow(cardId, string.Empty, string.Empty, false, false, 0, 0, 0);
        }

        public static bool TryFlip(FlipCardService service, int playerId, int cardId)
        {
            if (service == null || playerId <= 0 || cardId <= 0) return false;
            return service.TryFlip(playerId, cardId);
        }

        public static int GetMatchedPairs(FlipCardService service, int playerId)
        {
            if (service == null || playerId <= 0) return 0;
            return service.GetMatchedPairs(playerId);
        }

        public static bool IsComplete(FlipCardService service, int playerId)
        {
            if (service == null || playerId <= 0) return false;
            return service.IsComplete(playerId);
        }
    }
}
