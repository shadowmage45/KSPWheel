using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDebugOld : KSPWheelSubmodule
    {

        [KSPField(guiName = "Hit", guiActive = true)]
        public string colliderHit;

        [KSPField(guiName = "RPM", guiActive = true)]
        public float rpm;

        [KSPField(guiName = "fLong", guiActive = true)]
        public float fLong;

        [KSPField(guiName = "fLat", guiActive = true)]
        public float fLat;

        [KSPField(guiName = "fSpring", guiActive = true)]
        public float fSpring;

        [KSPField(guiName = "fSpringExt", guiActive = true)]
        public float fSpringExt;

        [KSPField(guiName = "comp", guiActive = true)]
        public float comp;

        [KSPField(guiName = "spr", guiActive = true)]
        public float spr;

        [KSPField(guiName = "dmp", guiActive = true)]
        public float dmp;

        [KSPField(guiName = "longFrict", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 3, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float longTractMult = 1f;

        [KSPField(guiName = "latFrict", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 3, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float latTractMult = 1f;

        [KSPField(guiName = "latSlip", guiActive = true, guiActiveEditor = false, isPersistant = false),
         UI_ProgressBar(minValue = 0, maxValue = 1)]
        public float latSlip = 0f;

        [KSPField(guiName = "longSlip", guiActive = true, guiActiveEditor = false, isPersistant = false),
         UI_ProgressBar(minValue = 0, maxValue = 1)]
        public float longSlip = 0f;

        /// <summary>
        /// The visual offset to the suspension transform compared to its default location and the wheel-colliders location.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "off"),
         UI_FloatEdit(suppressEditorShipModified = true, minValue = -5, maxValue = 5, incrementSmall = 0.25f, incrementLarge = 1f, incrementSlide = 0.0125f, sigFigs = 4)]
        public float suspensionOffset = 0f;

        [KSPField(guiActive = true, guiName = "Sweep Type", isPersistant = true),
         UI_ChooseOption(display = new string[] { "RAY", "SPHERE", "CAPSULE" }, options = new string[] { "RAY", "SPHERE", "CAPSULE" }, suppressEditorShipModified = true )]
        public string sweepType = KSPWheelSweepType.RAY.ToString();

        private KSPWheelSuspension suspension;
        private GameObject debugHitObject;

        private void suspensionOffsetChanged(BaseField field, System.Object obj)
        {
            if (suspension != null) { suspension.suspensionOffset = suspensionOffset; }
        }

        private void sweepTypeUpdated(BaseField field, System.Object obj)
        {
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                KSPWheelCollider wheel = controller.wheelData[i].wheel;
                wheel.sweepType = (KSPWheelSweepType)Enum.Parse(typeof(KSPWheelSweepType), sweepType);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);            
        }

        public void Start()
        {
            suspension = part.GetComponent<KSPWheelSuspension>();
            if (suspension == null)
            {
                Fields[nameof(suspensionOffset)].guiActive = false;
                Fields[nameof(suspensionOffset)].guiActiveEditor = false;
            }
            else
            {
                suspensionOffset = suspension.suspensionOffset;
            }
            Fields[nameof(suspensionOffset)].uiControlEditor.onFieldChanged = suspensionOffsetChanged;
            Fields[nameof(suspensionOffset)].uiControlFlight.onFieldChanged = suspensionOffsetChanged;
            Fields[nameof(sweepType)].uiControlFlight.onFieldChanged = sweepTypeUpdated;
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();

        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            debugHitObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider c = debugHitObject.GetComponent<Collider>();
            GameObject.Destroy(c);
            debugHitObject.transform.NestToParent(part.transform);
            debugHitObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            wheel.sweepType = (KSPWheelSweepType)Enum.Parse(typeof(KSPWheelSweepType), sweepType);
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                KSPWheelCollider wheel = controller.wheelData[i].wheel;
                wheel.forwardFrictionCoefficient = longTractMult;
                wheel.sideFrictionCoefficient = latTractMult;
            }
        }

        internal override void postWheelPhysicsUpdate()
        {
            base.postWheelPhysicsUpdate();
            //update gui debug values
            spr = wheel.spring;
            dmp = wheel.damper;
            fLong = wheel.longitudinalForce;
            fLat = wheel.lateralForce;
            fSpring = wheel.springForce;
            fSpringExt = 0;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                fSpringExt += controller.wheelData[i].wheel.externalSpringForce;
            }
            rpm = wheel.rpm;
            comp = wheel.compressionDistance;
            latSlip = wheel.lateralSlip;
            longSlip = wheel.longitudinalSlip;            
            colliderHit = wheel.isGrounded ? wheel.contactColliderHit.gameObject.name + " : " + wheel.contactColliderHit.gameObject.layer : "None";
            debugHitObject.transform.position = wheelTransform.position - (wheelTransform.up * wheel.length) + (wheelTransform.up * wheel.compressionDistance) - (wheelTransform.up * wheel.radius);
        }

    }
}
