﻿using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using System.Diagnostics;
using UnityEngine;

namespace Klyte.Commons
{

    public abstract class BasicVehicleTool<T> : DefaultTool where T : BasicVehicleTool<T>
    {

        protected override void Awake()
        {
            m_toolController = UnityEngine.Object.FindObjectOfType<ToolController>();
            base.enabled = false;
            instance = (T)this;
        }

        protected override void OnToolGUI(Event e)
        {
            if (UIView.HasModalInput() || UIView.HasInputFocus())
            {
                return;
            }
        }

        protected override void OnEnable()
        {
            InfoManager.InfoMode currentMode = Singleton<InfoManager>.instance.CurrentMode;
            InfoManager.SubInfoMode currentSubMode = Singleton<InfoManager>.instance.CurrentSubMode;
            m_prevRenderZones = Singleton<TerrainManager>.instance.RenderZones;
            m_toolController.CurrentTool = this;
            Singleton<InfoManager>.instance.SetCurrentMode(currentMode, currentSubMode);
            Singleton<TerrainManager>.instance.RenderZones = false;
        }

        protected override void OnDisable() => Singleton<TerrainManager>.instance.RenderZones = m_prevRenderZones;


        protected override void OnToolUpdate()
        {
            var isInsideUI = m_toolController.IsInsideUI;
            if (m_leftClickTime == 0L && Input.GetMouseButton(0) && !isInsideUI)
            {
                m_leftClickTime = Stopwatch.GetTimestamp();
                OnLeftMouseDown();
            }
            if (m_leftClickTime != 0L)
            {
                var num = ElapsedMilliseconds(m_leftClickTime);
                if (!Input.GetMouseButton(0))
                {
                    m_leftClickTime = 0L;
                    if (num < 200L)
                    {
                        OnLeftClick();
                    }
                    else
                    {
                        OnLeftDragStop();
                    }
                    OnLeftMouseUp();
                }
                else if (num >= 200L)
                {
                    OnLeftDrag();
                }
            }
            if (m_rightClickTime == 0L && Input.GetMouseButton(1) && !isInsideUI)
            {
                m_rightClickTime = Stopwatch.GetTimestamp();
                OnRightMouseDown();
            }
            if (m_rightClickTime != 0L)
            {
                var num2 = ElapsedMilliseconds(m_rightClickTime);
                if (!Input.GetMouseButton(1))
                {
                    m_rightClickTime = 0L;
                    if (num2 < 200L)
                    {
                        OnRightClick();
                    }
                    else
                    {
                        OnRightDragStop();
                    }
                    OnRightMouseUp();
                }
                else if (num2 >= 200L)
                {
                    OnRightDrag();
                }
            }
            if (!isInsideUI && Cursor.visible)
            {
                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                m_hoverVehicle = 0;
                RaycastHoverInstance(mouseRay);
            }
        }

        protected virtual void OnRightDrag() { }
        protected virtual void OnRightMouseUp() { }
        protected virtual void OnRightDragStop() { }
        protected virtual void OnRightClick() { }
        protected virtual void OnRightMouseDown() { }
        protected virtual void OnLeftDrag() { }
        protected virtual void OnLeftMouseUp() { }
        protected virtual void OnLeftDragStop() { }
        protected virtual void OnLeftClick() { }
        protected virtual void OnLeftMouseDown() { }

        protected override void OnToolLateUpdate() { }

        public override void SimulationStep() { }

        public override ToolBase.ToolErrors GetErrors() => ToolBase.ToolErrors.None;

        public bool m_trailersAlso = true;

        private void RaycastHoverInstance(Ray mouseRay)
        {
            var input = new ToolBase.RaycastInput(mouseRay, Camera.main.farClipPlane);
            Vector3 origin = input.m_ray.origin;
            Vector3 normalized = input.m_ray.direction.normalized;
            Vector3 vector = input.m_ray.origin + (normalized * input.m_length);
            var ray = new Segment3(origin, vector);

            VehicleManager.instance.RayCast(ray, 0, 0, out _, out m_hoverVehicle, out m_hoverParkedVehicle);

        }
        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, ushort vehicleId, bool parked)
        {
            if (vehicleId == 0)
            {
                return;
            }
            if (m_trailersAlso && !parked)
            {
                var subVehicle = VehicleBuffer[vehicleId].GetFirstVehicle(vehicleId);
                while (subVehicle > 0)
                {
                    HoverVehicle(cameraInfo, toolColor, subVehicle);
                    subVehicle = VehicleBuffer[subVehicle].m_trailingVehicle;
                }
            }
            else
            {
                if (parked)
                {
                    HoverParkedVehicle(cameraInfo, toolColor, vehicleId);
                }
                else
                {
                    HoverVehicle(cameraInfo, toolColor, vehicleId);
                }
            }
        }

        private static void HoverVehicle(RenderManager.CameraInfo cameraInfo, Color toolColor, ushort vehicleId) => VehicleBuffer[vehicleId].RenderOverlay(cameraInfo, vehicleId, toolColor);
        private static void HoverParkedVehicle(RenderManager.CameraInfo cameraInfo, Color toolColor, ushort vehicleId) => VehicleParkedBuffer[vehicleId].RenderOverlay(cameraInfo, vehicleId, toolColor);

        private long ElapsedMilliseconds(long startTime)
        {
            var timestamp = Stopwatch.GetTimestamp();
            long num = timestamp > startTime ? timestamp - startTime : startTime - timestamp;
            return num / (Stopwatch.Frequency / 1000L);
        }

        protected static ref Vehicle[] VehicleBuffer => ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
        protected static ref VehicleParked[] VehicleParkedBuffer => ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer;


        public static T instance;


        protected static Color m_hoverColor = new Color32(47, byte.MaxValue, 47, byte.MaxValue);

        protected static Color m_removeColor = new Color32(byte.MaxValue, 47, 47, 191);
        protected static Color m_despawnColor = new Color32(byte.MaxValue, 160, 47, 191);

        public static Shader shaderBlend = Shader.Find("Custom/Props/Decal/Blend");

        public static Shader shaderSolid = Shader.Find("Custom/Props/Decal/Solid");

        protected ushort m_hoverVehicle;
        protected ushort m_hoverParkedVehicle;

        private bool m_prevRenderZones;

        private long m_rightClickTime;

        private long m_leftClickTime;



    }

}
