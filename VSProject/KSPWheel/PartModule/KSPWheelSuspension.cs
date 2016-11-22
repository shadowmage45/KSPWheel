using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelSuspension : PartModule
    {
        [KSPField]
        public string suspensionName = "suspension";

        [KSPField]
        public string wheelColliderName = "wheelCollider";

        [KSPField]
        public int indexInDuplicates = 0;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "off"),
         UI_FloatEdit(suppressEditorShipModified = true, minValue = -5, maxValue = 5, incrementSmall = 0.25f, incrementLarge = 1f, incrementSlide = 0.025f, sigFigs = 2)]
        public float suspensionOffset = 0f;

        [KSPField]
        public Vector3 suspensionAxis = Vector3.up;

        private Vector3 defaultPos;
        private Transform suspensionTransform;
        private Transform wheelColliderTransform;
        private KSPWheelCollider wheel;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Transform[] sus = part.transform.FindChildren(suspensionName);
            Transform[] wcs = part.transform.FindChildren(wheelColliderName);
            suspensionTransform = sus[indexInDuplicates];
            defaultPos = suspensionTransform.localPosition;
            wheelColliderTransform = wcs[indexInDuplicates];
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            if (wheel == null) { wheel = wheelColliderTransform.GetComponent<KSPWheelCollider>(); return; }
            float scale = suspensionTransform.parent.localScale.y;
            float offset = (wheel.length - wheel.compressionDistance + suspensionOffset) / scale;
            suspensionTransform.localPosition = defaultPos - suspensionAxis * offset;
        }

    }
}
