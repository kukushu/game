using UnityEngine;
using UnityEngine.InputSystem;
namespace HarborCity
{
    // Restored construction controls, independent of city entities or transport.
    // Uses the original cursor-plane pan/zoom idea; no simulation position writes.
    internal sealed class HarborCityConstructionCamera
    {
        readonly Camera camera;readonly CityLandscape landscape;
        Vector3 focus;float distance=110,pitch=55,yaw;Vector2 previous;
        bool panning,rotating;
        public HarborCityConstructionCamera(Camera camera,CityLandscape landscape){this.camera=camera;this.landscape=landscape;Apply();}
        void Apply(){focus.x=Mathf.Clamp(focus.x,-CityModel.BuildHalfSize,CityModel.BuildHalfSize);focus.z=Mathf.Clamp(focus.z,-CityModel.BuildHalfSize,CityModel.BuildHalfSize);focus.y=Mathf.Max(0,landscape.Height(focus.x,focus.z));camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);camera.transform.position=focus-camera.transform.forward*distance;}
        Vector3 Anchor(Vector2 pointer){var ray=camera.ScreenPointToRay(pointer);return landscape.Raycast(ray,out var hit)?hit:focus;}
        void Pan(Vector2 from,Vector2 to)
        {var plane=new Plane(Vector3.up,Anchor(from));var a=camera.ScreenPointToRay(from);var b=camera.ScreenPointToRay(to);if(plane.Raycast(a,out float da)&&plane.Raycast(b,out float db)){focus+=a.GetPoint(da)-b.GetPoint(db);Apply();}}
        public void Focus(float x,float z){focus=new Vector3(x,0,z);Apply();}
        public void Step(float dt)
        {
            if(!Application.isFocused){panning=rotating=false;return;}var keyboard=Keyboard.current;
            if(keyboard!=null){var local=new Vector3((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),0,(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));if(local.sqrMagnitude>0){focus+=Quaternion.Euler(0,yaw,0)*local.normalized*dt*Mathf.Max(20,distance*.4f);Apply();}float turn=(keyboard.eKey.isPressed?1:0)-(keyboard.qKey.isPressed?1:0);if(turn!=0){yaw=Mathf.Repeat(yaw+turn*dt*60,360);Apply();}}
            var mouse=Mouse.current;if(mouse==null)return;var pointer=mouse.position.ReadValue();
            if(mouse.middleButton.wasPressedThisFrame){panning=true;previous=pointer;}if(mouse.rightButton.wasPressedThisFrame){rotating=true;previous=pointer;}
            if(panning&&mouse.middleButton.isPressed)Pan(previous,pointer);
            if(rotating&&mouse.rightButton.isPressed){var delta=pointer-previous;yaw=Mathf.Repeat(yaw+delta.x*.25f,360);pitch=Mathf.Clamp(pitch-delta.y*.25f,20,80);Apply();}
            if(!mouse.middleButton.isPressed)panning=false;if(!mouse.rightButton.isPressed)rotating=false;previous=pointer;
            float scroll=mouse.scroll.ReadValue().y;
            if(InputSystem.settings.scrollDeltaBehavior==InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange&&(Application.platform==RuntimePlatform.WindowsEditor||Application.platform==RuntimePlatform.WindowsPlayer))scroll/=120;
            if(Mathf.Abs(scroll)<.001)return;var anchor=Anchor(pointer);distance=Mathf.Clamp(distance*Mathf.Exp(-scroll*.16f),12,600);Apply();var planeAtAnchor=new Plane(Vector3.up,anchor);var rayAfter=camera.ScreenPointToRay(pointer);if(planeAtAnchor.Raycast(rayAfter,out float after)){focus+=anchor-rayAfter.GetPoint(after);Apply();}
        }
    }
}
