using UnityEngine;
using Sapphire.Domain.Combat;
using Sapphire.Presentation.World;

namespace Sapphire.Presentation.Combat
{
    public class MonsterController : MonoBehaviour
    {
        public CombatStats Stats { get; private set; }
        public HealthComponent Health { get; private set; }
        
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        public void Initialize(int x, int y, int maxHp, int atk, int def)
        {
            GridX = x;
            GridY = y;
            Stats = new CombatStats(maxHp, 0, atk, def);
            Health = new HealthComponent(maxHp);
            
            Health.OnDied += HandleDeath;
        }

        private void HandleDeath()
        {
            // Remove blocker from collision tilemap
            var interactable = GetComponent<InteractableZone>();
            if (interactable != null)
            {
                // In a full implementation, we'd remove it from InteractionMap and clear the Tilemap blocker.
                // For this slice, destroying the GameObject handles the visual and leaves the tilemap blocking,
                // but we should ideally clear the blocker tile. We'll do a quick FindObjectOfType for now.
                var gridBuilder = FindObjectOfType<TilemapGridMapBuilder>();
                if (gridBuilder != null)
                {
                    var collisionMap = (UnityEngine.Tilemaps.Tilemap)gridBuilder.GetType().GetField("collisionTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(gridBuilder);
                    collisionMap.SetTile(new Vector3Int(GridX, GridY, 0), null);
                }
            }
            
            Destroy(gameObject);
        }
    }
}
