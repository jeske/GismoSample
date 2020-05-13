using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.DebugDrawer
{
    internal static class Materials
    {
        public static Material CreateDebugMaterial(Color color, bool emissive, GraphicsDevice device)
        {
            var descriptor = new MaterialDescriptor();
            var computeColor = new ComputeColor(color);
            if (emissive)
            {
                descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(computeColor);
            }
            else
            {
                descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(computeColor);
                descriptor.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();
            }

            return Material.New(device, descriptor);
        }
    }
}