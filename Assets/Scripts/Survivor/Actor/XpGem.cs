using UnityEngine;

namespace VLTK.Survivor
{
    public sealed class XpGem : MonoBehaviour
    {
        public int amount = 1;

        private void Awake()
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProxyVisuals.White();
            sr.color = new Color(0.4f, 0.9f, 1f);
            sr.drawMode = SpriteDrawMode.Simple;
            transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        }

        public void Init(Vector3 pos, int n)
        {
            transform.position = pos;
            amount = n;
        }

        private void Update()
        {
            var player = SurvivorGameDirector.Instance.Player;
            if (player == null) return;
            var d = player.transform.position - transform.position;
            float dist = d.magnitude;
            if (dist < player.PickupRadius)
            {
                var step = Mathf.Min(8f * Time.deltaTime, dist);
                transform.position += (Vector3)(d.normalized * step);
            }
            if (dist < 0.4f)
            {
                player.AddXp(amount);
                SurvivorGameDirector.Instance.OnGemCollected(this);
                Destroy(gameObject);
            }
        }
    }
}
