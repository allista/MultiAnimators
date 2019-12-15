//   MultiGeometryAnimator.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using UnityEngine;

namespace AT_Utils
{
    /// <summary>
    /// This variant of MultiAnimator implements the MultipleDragCube interface
    /// and should be used to animate geometry changes, e.g. moving doors, etc.
    /// </summary>
    public class MultiGeometryAnimator : MultiAnimator, IMultipleDragCube
    {
        [KSPField] public string DragCubeA = "A";
        [KSPField] public string DragCubeB = "B";
        [KSPField] [SerializeField] public VectorCurve CoMCurve = new VectorCurve();

        public string[] GetDragCubeNames()
        {
            return new[] { DragCubeA, DragCubeB };
        }

        public void AssumeDragCubePosition(string drag_cube_name)
        {
            setup_animation();
            if(drag_cube_name == DragCubeA)
                seek(0, false);
            else if(drag_cube_name == DragCubeB)
                seek(1, false);
        }

        public bool UsesProceduralDragCubes()
        {
            return false;
        }

        public bool IsMultipleCubesActive { get { return true; } }

        protected override void on_norm_time(float t)
        {
            part.DragCubes.SetCubeWeight(DragCubeA, t);
            part.DragCubes.SetCubeWeight(DragCubeB, 1 - t);
            if(part.DragCubes.Procedural)
                part.DragCubes.ForceUpdate(true, true, false);
            if(CoMCurve.Length > 0)
                part.CoMOffset = CoMCurve.Evaluate(t);
        }

        public void UpdateCoMOffset()
        {
            if(CoMCurve.Length == 0)
                return;
            part.CoMOffset = CoMCurve.Evaluate(n_time);
        }
    }

    public class GeometryAnimatorUpdater : ModuleUpdater<MultiGeometryAnimator>
    {
        protected override void on_rescale(ModulePair<MultiGeometryAnimator> mp, Scale scale)
        {
            mp.module.EnergyConsumption = mp.base_module.EnergyConsumption
                                          * scale.absolute.quad
                                          * scale.absolute.aspect;
            if(mp.module.CoMCurve != null)
            {
                mp.module.CoMCurve.Scale(scale.ScaleVectorRelative(Vector3d.one));
                mp.module.UpdateCoMOffset();
            }
        }
    }
}
