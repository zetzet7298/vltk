// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.2 NPC Dialogue Service
// NPC interactive dialogue trees, option selection, quest state queries.
// Source: PC NPC dialogue flows, localizing scripts to Vietnamese.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Core;

namespace VLTK.Sandbox
{
    [Serializable]
    public struct DialogueOption
    {
        public string textVi;
        public int targetNodeId;
        public Action selectAction;
        public Func<bool> condition; // Điều kiện để tùy chọn này xuất hiện
    }

    [Serializable]
    public class DialogueNode
    {
        public int nodeId;
        public string npcTextVi;
        public List<DialogueOption> options = new();
    }

    /// <summary>
    /// Service quản lý hội thoại với NPC.
    /// Tương thích với quest states (Dã Tẩu, Xa Phu, Gia nhập môn phái).
    /// </summary>
    public class NpcDialogueService
    {
        private readonly TaskFlagService _taskService;
        private readonly Dictionary<int, List<DialogueNode>> _npcDialogues = new();

        public event Action<DialogueNode> OnDialogueStarted;
        public event Action OnDialogueEnded;

        public NpcDialogueService(TaskFlagService taskService)
        {
            _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        }

        /// <summary>
        /// Tạo hội thoại mặc định cho NPC dựa trên templateId.
        /// Việt hóa nội dung chào hỏi của Võ Sư, Dã Tẩu, Xa Phu, v.v.
        /// </summary>
        public DialogueNode StartDialogue(int npcTemplateId, int playerLevel)
        {
            // Trả về cây hội thoại của NPC
            if (!_npcDialogues.TryGetValue(npcTemplateId, out var nodes))
            {
                nodes = BuildDefaultDialogueNodes(npcTemplateId, playerLevel);
                _npcDialogues[npcTemplateId] = nodes;
            }

            var rootNode = nodes[0];
            // Filter options according to condition
            var filteredNode = new DialogueNode
            {
                nodeId = rootNode.nodeId,
                npcTextVi = rootNode.npcTextVi
            };

            foreach (var opt in rootNode.options)
            {
                if (opt.condition == null || opt.condition())
                {
                    filteredNode.options.Add(opt);
                }
            }

            OnDialogueStarted?.Invoke(filteredNode);
            return filteredNode;
        }

        /// <summary>
        /// Chọn một option trong hội thoại của NPC.
        /// </summary>
        public DialogueNode SelectOption(int npcTemplateId, int playerLevel, DialogueOption option)
        {
            option.selectAction?.Invoke();

            if (option.targetNodeId <= 0)
            {
                CloseDialogue();
                return null;
            }

            if (_npcDialogues.TryGetValue(npcTemplateId, out var nodes))
            {
                var next = nodes.Find(n => n.nodeId == option.targetNodeId);
                if (next != null)
                {
                    var filteredNode = new DialogueNode
                    {
                        nodeId = next.nodeId,
                        npcTextVi = next.npcTextVi
                    };
                    foreach (var opt in next.options)
                    {
                        if (opt.condition == null || opt.condition())
                            filteredNode.options.Add(opt);
                    }
                    OnDialogueStarted?.Invoke(filteredNode);
                    return filteredNode;
                }
            }

            CloseDialogue();
            return null;
        }

        public void CloseDialogue()
        {
            OnDialogueEnded?.Invoke();
        }

        // ── Dialogue Builders ──────────────────────────────────────────────

        private List<DialogueNode> BuildDefaultDialogueNodes(int npcTemplateId, int playerLevel)
        {
            var nodes = new List<DialogueNode>();

            // 1) NPC Dã Tẩu (Giả định templateId = 500)
            if (npcTemplateId == 500)
            {
                var root = new DialogueNode
                {
                    nodeId = 1,
                    npcTextVi = "Dã Tẩu: Ta có rất nhiều việc cần giang hồ hiệp sĩ giúp đỡ. Ngươi có muốn nhận nhiệm vụ không?"
                };

                // Option nhận quest
                root.options.Add(new DialogueOption
                {
                    textVi = "Nhận nhiệm vụ Dã Tẩu",
                    targetNodeId = 2,
                    condition = () => _taskService.GetFlag(1000) == 0, // Chưa nhận nhiệm vụ 1000
                    selectAction = () => _taskService.SetFlag(1000, 1, 0, 5, "Tiêu diệt 5 Mèo Vàng ngoài thành")
                });

                // Option trả quest
                root.options.Add(new DialogueOption
                {
                    textVi = "Hoàn thành nhiệm vụ (Báo cáo)",
                    targetNodeId = 3,
                    condition = () => _taskService.IsTaskComplete(1000), // Đã hoàn thành
                    selectAction = () => _taskService.SetFlag(1000, 3) // Trả quest nhận thưởng
                });

                root.options.Add(new DialogueOption
                {
                    textVi = "Chỉ ghé ngang qua",
                    targetNodeId = 0
                });

                nodes.Add(root);

                // Node 2: Nhận nhiệm vụ xong
                nodes.Add(new DialogueNode
                {
                    nodeId = 2,
                    npcTextVi = "Dã Tẩu: Rất tốt, ngoài thành có rất nhiều Mèo Vàng quấy nhiễu dân làng, ngươi hãy đi tiêu diệt 5 con giúp ta."
                });

                // Node 3: Trả nhiệm vụ xong
                nodes.Add(new DialogueNode
                {
                    nodeId = 3,
                    npcTextVi = "Dã Tẩu: Cảm ơn hiệp sĩ! Đây là phần thưởng xứng đáng dành cho ngươi."
                });
            }
            // 2) NPC Võ Sư (templateId = 311)
            else if (npcTemplateId == 311)
            {
                var root = new DialogueNode
                {
                    nodeId = 1,
                    npcTextVi = "Võ Sư: Luyện tập võ nghệ giúp rèn luyện gân cốt. Ngươi muốn luyện tập thế nào?"
                };

                root.options.Add(new DialogueOption
                {
                    textVi = "Tẩy tủy điểm tiềm năng",
                    targetNodeId = 2,
                    selectAction = () => SubsystemLog.Info("Dialogue", "Player reset potential points.")
                });

                root.options.Add(new DialogueOption
                {
                    textVi = "Rời đi",
                    targetNodeId = 0
                });

                nodes.Add(root);

                nodes.Add(new DialogueNode
                {
                    nodeId = 2,
                    npcTextVi = "Võ Sư: Điểm tiềm năng của ngươi đã được tẩy tủy hoàn toàn."
                });
            }
            // 3) NPC Xa Phu (templateId = 501)
            else if (npcTemplateId == 501)
            {
                var root = new DialogueNode
                {
                    nodeId = 1,
                    npcTextVi = "Xa Phu: Ngươi muốn di chuyển đi đâu? Bản phu xa có thể đưa ngươi tới các tân thủ thôn hoặc môn phái."
                };

                root.options.Add(new DialogueOption
                {
                    textVi = "Đến Biện Kinh Phủ (20 lượng)",
                    targetNodeId = 0,
                    condition = () => playerLevel >= 10
                });

                root.options.Add(new DialogueOption
                {
                    textVi = "Ở lại đây",
                    targetNodeId = 0
                });

                nodes.Add(root);
            }
            // 4) NPC Khác (Mặc định)
            else
            {
                var root = new DialogueNode
                {
                    nodeId = 1,
                    npcTextVi = "NPC: Chào hiệp sĩ, giang hồ hiểm ác, đi đứng cẩn thận."
                };
                root.options.Add(new DialogueOption { textVi = "Tạm biệt", targetNodeId = 0 });
                nodes.Add(root);
            }

            return nodes;
        }
    }
}
