using System.Collections.Generic;
using UnityEngine;

namespace Sapphire.Presentation.World
{
    public class AmbientDustSpawner : MonoBehaviour
    {
        private List<GameObject> particles = new List<GameObject>();
        public Color ParticleColor { get; set; } = Color.white;
        public Sprite DustSprite { get; set; }
        
        private void Start()
        {
            var bounds = new Bounds(transform.position, new Vector3(30, 30, 0));
            
            for (int i = 0; i < 40; i++)
            {
                var go = new GameObject("AmbientParticle", typeof(SpriteRenderer));
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    0f);
                go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.15f);
                
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = DustSprite;
                sr.color = new Color(ParticleColor.r, ParticleColor.g, ParticleColor.b, Random.Range(0.2f, 0.6f));
                sr.sortingOrder = 32000;
                
                particles.Add(go);
            }
        }

        private void Update()
        {
            float t = Time.time;
            foreach (var p in particles)
            {
                if (p == null) continue;
                Vector3 pos = p.transform.position;
                pos.x += Mathf.Sin(t + pos.y * 5f) * Time.deltaTime * 0.3f;
                pos.y -= Time.deltaTime * 0.4f;
                
                if (pos.y < transform.position.y - 15f)
                {
                    pos.y += 30f;
                    pos.x = transform.position.x + Random.Range(-15f, 15f);
                }
                
                p.transform.position = pos;
            }
        }
    }
}
