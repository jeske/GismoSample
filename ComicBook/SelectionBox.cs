using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace ComicBook
{
    class SelectionBox
    {
        List<Entity> entityCorners;
        Entity entity;
        float boxExtent;

        public SelectionBox(Prefab prefab, float extent = 1.0f) 
        {
            entityCorners = prefab.Instantiate();
            boxExtent = extent;
        }

        public void SetMaterial(Material material)
        {
            for (int i = 0; i < entityCorners.Count; i++)
            {
                entityCorners[i].Get<ModelComponent>().Materials[0] = material;
            }
        }

        public Entity Entity {
            get { return entity; }
            set {
                if (entity != value)
                {
                    if (entity != null)
                    {
                        for (int i = 0; i < entityCorners.Count; i++)
                        {
                            entity.RemoveChild(entityCorners[i]);
                        }
                    }

                    entity = value;

                    if (entity != null)
                    {
                        Vector3[] corners = entity.Get<ModelComponent>().Model.BoundingBox.GetCorners();
                        for (int i = 0; i < entityCorners.Count; i++)
                        {
                            entity.AddChild(entityCorners[i]);
                            entityCorners[i].Transform.Position = corners[i] * boxExtent;
                            entityCorners[i].Transform.Scale = new Vector3(0.1f);
                        }
                    }
                }
            }
        }
    }
}
