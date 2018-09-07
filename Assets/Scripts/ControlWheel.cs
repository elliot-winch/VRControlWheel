using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * ControlWheel
 * 
 * This class is used to produce a Control Wheel, a circular, segmented action selector. It is especially useful for VR, where button inputs are limited
 * and non-diegetic UI is more appropriate.
 * 
 * You can attach this component to any GameObject. It can be controlled by another, more user-specific script by calling: 
 * 	AddControlWheelAction / AddControlWheelActions		(one / mant ControlWheelSegments)
 *  RemoveControlWheelAction							(string name)
 *  DisplayControlWheel									()
 *  HideControlWheel									()
 *  HighlightSectionAtLocation							(Vector2 locationInCircle)
 * 	Select												(Vector2 locationInCircle)
 * 
 * 
 * See the Demo scene and ConsolePrinter.cs for an example of using the Control Wheel.
 * 
 * Preferred Position:
 * 	If a segment is initialised with a preferred postion, the ControlWheel will tyr its best to honour the segment's position. However, for an odd number of segments, the segment
 *  might not appear where intended. Also, if two segments have the same preferred position, the one added first will always be assigned the position and any subsequent will be
 *  treated as if they had no preferred position.
 */
namespace FactualVR.HyperTunnel.UI
{
    public class ControlWheel : MonoBehaviour
    {

        //The radius of the external circumference that defines the ControlWheel
        public float farRadius = 1f;
        //The radius of the internal circumference that defines the ControlWheel
        public float nearRadius = 0.5f;
        //The distance from the center of the ControlWheel the icons lie. N.B  if NOT (nearRadius < iconDistance < farRadius), then your icons will not lie on the segment.
        public float iconDistance = 0.7f;

        //N.B. the two variables farRadius and nearRadius effectively define the thickness of the segments.

        //How far away the ControlWheel Segment should extend when it is highlighted
        public float highlightDistance = 0.1f;
        //The increase in size of a segment when it is highlighted
        public float highlightedScale = 1.3f;

        //The number of verticies that will be used to create the ControlWheel. N.B this is the number of vertices in all segments, NOT per segment.
        //Increasing this value will create a sharper looking circle. The gains in resolution aren't very noticable past 60.
        public int circumferenceVertices = 60;

        //The material the segments will use to render
        public Material displaySegmentMaterial;
        //An optional parameter. Can set the parent of the segments to something other than the current parent.
        public Transform parent = null;

        //A prefab for the segment label
        public GameObject labelPrefab;

        //Is the ControlWheel displaying? If so, the user can select an action to perform.
        private bool active = false;
        //Getter
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                active = value;

                if(displaySegments != null)
                {
                    foreach (GameObject seg in displaySegments)
                    {
                        seg.SetActive(active);
                    }
                }
            }
        }

        //A list of segments
        List<ControlWheelSegment> cwActions;
        //The vectors which define the boundaries for each segment. Used for quicker calculations of clockwise/ anti-clockwise.
        List<Vector2> dividingVectors;
        //THe actual GameObjects that exist in scene for each segment
        List<GameObject> displaySegments;

        #region Modifications to the ControlWheel's Segments
        /// This region is for methods used to manipulate the ControlWheel's segments, called by the user.

        /**
         * AddControlWheelAction
         * Adds a new ControlWheelSegment to the ControlWheel
         */
        public void AddControlWheelAction(ControlWheelSegment cwa)
        {
            //Lazy init
            if (cwActions == null)
            {
                cwActions = new List<ControlWheelSegment>();
            }

            cwActions.Add(cwa);

            //Recreates the control wheel
            CreateControlWheel();
        }

        /**
         * AddControlWheelActions
         * Used to add multiple ControlWheelSegments to the ControlWheel. 
         * If you are adding more than one segment at a time, it is recommended you use this function over AddControlWheelAction, so as to not recreate the ControlWheel needlessly.
         */
        public void AddControlWheelActions(ControlWheelSegment[] segs)
        {
            if (cwActions == null)
            {
                cwActions = new List<ControlWheelSegment>();
            }

            for (int i = 0; i < segs.Length; i++)
            {
                cwActions.Add(segs[i]);
            }

            //Recreate the control wheel
            CreateControlWheel();
        }

        /**
         * RemoveControlWheelAction
         * Removes a ControlWheelSegment by its name. If you intend to remove a segment, it must be named when it is created.
         */
        public void RemoveControlWheelAction(string name)
        {

            foreach (ControlWheelSegment seg in cwActions)
            {
                if (seg.Name.Equals(name))
                {
                    cwActions.Remove(seg);

                    //Recreate the control wheel
                    CreateControlWheel();
                    return;
                }
            }

            Debug.LogWarning("Warning! Failed to find a segment with the name: " + name);
        }

        /// <summary>
        /// Destroys current control wheel and resets all attributes on control wheel
        /// </summary>
        public void ResetControlWheel()
        {
            if (displaySegments != null)
            {
                foreach (GameObject seg in displaySegments)
                {
                    Destroy(seg);
                }
            }

            if(displaySegments != null)
            {
                displaySegments.Clear();
            }

            if(dividingVectors != null)
            {
                dividingVectors.Clear();
            }

            if(cwActions != null)
            {
                cwActions.Clear();
            }
        }

        #endregion

        #region Interactions With The Existing ControlWheel

        //Cached segement that is current highlighted, used to RemoveHighlight
        GameObject prevHighlight;

        /**
         * HighlightSectionAtLocation
         * Used to inform the user that they are hovering over a segment. 
         * 
         * The location given should be a co-ordinate, expressing the user's input on a circle CENTERED AT (0,0)	
         * N.B. SteamVR_Controller.Device's input for the Oculus' thumbstick / Vive's touchpad does not need to be modified for this function.
         */
        public void HighlightSectionAtLocation(Vector2 locationInCircle)
        {

            RemoveHighlight();

            int sectorNum = GetSector(locationInCircle);

            if (sectorNum >= 0 && sectorNum < cwActions.Count)
            {
                displaySegments[sectorNum].transform.localScale = new Vector3(highlightedScale, highlightedScale, highlightedScale);

                prevHighlight = displaySegments[sectorNum];
            }
        }

        /**
         * RemoveHighlight
         * Helper for HighlightSectionAtLocation
         * Moves a highlighted segment back to its regular position
         */
        void RemoveHighlight()
        {

            if (prevHighlight != null)
            {
                prevHighlight.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        /**
         * Select
         * Calls the Action supplied by the user for a segment at a location.
         * 
         * The location given should be a co-ordinate, expressing the user's input on a circle CENTERED AT (0,0)	
         * N.B. SteamVR_Controller.Device's input for the Oculus' thumbstick / Vive's touchpad does not need to be modified for this function.
         */
        public void Select(Vector2 locationInCircle)
        {

            if (this.active == true)
            {

                int sectorNum = GetSector(locationInCircle);
                if (sectorNum >= 0 && sectorNum < cwActions.Count)
                {
                    Debug.Log(cwActions[sectorNum]);
                    cwActions[sectorNum].Action();
                }
            }
        }
        #endregion

        #region Private Methods 
        /**
         * CreateControlWheel
         * Creates the ControlWheel, from scratch everytime. Repeated calls to this function are expensive and unnecessary. A single call can easily be managed in a frame.
         */
        private void CreateControlWheel()
        {

            //Preferred Ordering
            ControlWheelSegment[] orderedSegs = new ControlWheelSegment[cwActions.Count];
            List<int> unsetOrdered = new List<int>();
            List<int> setUnordered = new List<int>();

            //Reorder based on preference
            for (int i = 0; i < cwActions.Count; i++)
            {
                for (int j = 0; j < cwActions.Count; j++)
                {

                    if (cwActions[j].PreferredIndex(cwActions.Count) == i)
                    {
                        if (orderedSegs[i] == null)
                        {
                            orderedSegs[i] = cwActions[j];
                            setUnordered.Add(j);
                        }
                        else
                        {
                            Debug.LogWarning("Control Wheel Warning: Preferred Position " + i + " is already set for " + name);
                        }
                    }
                }

                if (orderedSegs[i] == null)
                {
                    unsetOrdered.Add(i);
                }
            }

            List<int> unsetUnordered = new List<int>();
            for (int i = 0; i < cwActions.Count; i++)
            {
                if (setUnordered.Contains(i) == false)
                {
                    unsetUnordered.Add(i);
                }
            }

            for (int i = 0; i < unsetUnordered.Count; i++)
            {
                orderedSegs[unsetOrdered[i]] = cwActions[unsetUnordered[i]];
            }

            cwActions = orderedSegs.ToList();
            //Reordering complete

            //Destory previous wheel
            if(displaySegments != null)
            {
                foreach (GameObject seg in displaySegments)
                {
                    Destroy(seg);
                }
            }

            displaySegments = new List<GameObject>();
            dividingVectors = new List<Vector2>();

            if(cwActions.Count > 0)
            {
                //Create each segment
                int steps = (int)(circumferenceVertices / cwActions.Count);
                for (int i = 0; i < cwActions.Count; i++)
                {
                    float angle = 2 * Mathf.PI * i / cwActions.Count;
                    //shift angle to top of circle (ie by 90 degrees)
                    angle += Mathf.PI / 2;

                    //Shift by half of the size of one area
                    angle -= Mathf.PI * 2 / (cwActions.Count * 2);

                    float totalAngleDist = 2 * Mathf.PI / cwActions.Count;

                    GameObject segObj = CreateSegmentObject(angle, totalAngleDist, steps);
                    segObj.name += cwActions[i].Name;

                    displaySegments.Add(segObj);

                    AddIconToSegment(segObj, cwActions[i].Icon, angle, totalAngleDist);

                    if (cwActions[i].ShowLabel)
                    {
                        AddLabelToSegment(segObj, cwActions[i].Name, angle, totalAngleDist);
                    }

                    dividingVectors.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                }
            }
        }

        /**
         * CreateSegmentObject
         * Helper for CreateControlWheel
         * Creates the segments
         */
        private GameObject CreateSegmentObject(float startAngle, float angleDist, int steps)
        {

            GameObject seg = new GameObject();
            seg.name = "Control Wheel Segment: ";

            //Manually builds the mesh
            Vector3[] verts = new Vector3[(steps + 1) * 2];

            for (int i = 0; i <= steps; i++)
            {
                float angle = startAngle + ((float)i / steps) * angleDist;

                float s = Mathf.Sin(angle);
                float c = Mathf.Cos(angle);

                verts[i * 2] = new Vector3(farRadius * c, 0f, farRadius * s);
                //verts[i * 2 + 1] = 	new Vector3(farRadius * c, 0f, farRadius * s);
                verts[i * 2 + 1] = new Vector3(nearRadius * c, 0f, nearRadius * s);
                //verts[i * 2 + 3] = 	new Vector3(nearRadius * c, 0f, nearRadius * s);

            }

            int[] triangles = new int[steps * 6];

            int vertexIndex = 0;
            //i is triangle point index
            for (int i = 0; i < triangles.Length; i += 6)
            {

                triangles[i] = vertexIndex;
                triangles[i + 1] = vertexIndex + 1;
                triangles[i + 2] = vertexIndex + 2;

                triangles[i + 3] = vertexIndex + 1;
                triangles[i + 4] = vertexIndex + 3;
                triangles[i + 5] = vertexIndex + 2;

                vertexIndex += 2;
            }

            Vector3[] normals = new Vector3[verts.Length];

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.up;
            }

            Mesh m = new Mesh();
            m.vertices = verts;
            m.triangles = triangles;
            m.normals = normals;

            seg.AddComponent<MeshFilter>().sharedMesh = m;
            seg.AddComponent<MeshRenderer>().material = displaySegmentMaterial;

            seg.SetActive(this.Active);
            seg.transform.SetParent((parent != null) ? parent : transform.parent);
            seg.transform.localScale = new Vector3(1f, 1f, 1f);
            seg.transform.localRotation = Quaternion.identity;

            float halfAngle = startAngle + angleDist / 2f;
            seg.transform.localPosition = new Vector3(highlightDistance * Mathf.Cos(halfAngle), 0f, highlightDistance * Mathf.Sin(halfAngle));

            return seg;
        }

        /*
         * AddIconToSegment
         * Helper for CreateSegmentObject
         * Inits and adds a supplied icon to a segment
         */
        void AddIconToSegment(GameObject seg, Sprite icon, float startAngle, float totalAngleDist)
        {
            float halfAngle = startAngle + totalAngleDist / 2f;

            GameObject iconGo = new GameObject();
            iconGo.name = "Segment Icon";
            iconGo.AddComponent<SpriteRenderer>().sprite = icon;

            iconGo.transform.SetParent(seg.transform);
            iconGo.transform.localRotation = Quaternion.Euler(new Vector3(90f, 0f, 0f));
            iconGo.transform.localScale = new Vector3(1f, 1f, 1f);
            iconGo.transform.localPosition = new Vector3(iconDistance * Mathf.Cos(halfAngle), 0.1f, iconDistance * Mathf.Sin(halfAngle));
        }

        /*
         * AddLabelToSegment
         * Helper for CreateSegmentObject
         * Inits and adds a label to a segment
         */
        void AddLabelToSegment(GameObject seg, string text, float startAngle, float totalAngleDist)
        {
            float halfAngle = startAngle + totalAngleDist / 2f;

            GameObject label = Instantiate(labelPrefab, seg.transform);
            label.name = "Segment Label";

            label.GetComponent<TextMesh>().text = text;

            label.transform.localRotation = Quaternion.Euler(new Vector3(90f, 0f, 0f));
            label.transform.localPosition = new Vector3(iconDistance * Mathf.Cos(halfAngle), 0.3f, iconDistance * Mathf.Sin(halfAngle));
        }

        /*
         * GetSector
         * Helper for HighlightSectionAtLocation and Select
         * Gets the sectors from the location supplied.
         * 
         * The location given should be a co-ordinate, expressing the user's input on a circle CENTERED AT (0,0)	
         * N.B. SteamVR_Controller.Device's input for the Oculus' thumbstick / Vive's touchpad does not need to be modified for this function.
         */
        private int GetSector(Vector2 point)
        {

            if (cwActions == null || cwActions.Count <= 0)
            {
                return -1;
            }

            //only one sector is an edge case, since the only point will fail our clockwise / not clockwise test
            if (cwActions.Count == 1)
            {
                return 0;
            }


            for (int i = 0; i < cwActions.Count; i++)
            {
                Vector2 startVec = dividingVectors[i];
                Vector2 endVec = dividingVectors[(i + 1) % dividingVectors.Count];

                if (IsClockwise(startVec, point) == false && IsClockwise(endVec, point) == true)
                {
                    return (int)i;
                }
            }

            return -1;
        }

        /**
         * IsClockwise
         * Helper for GetSector
         * Determines if a vector is clockwise from another vector
         */
        private bool IsClockwise(Vector2 v1, Vector2 v2)
        {
            return -v1.x * v2.y + v1.y * v2.x >= 0;
        }
        #endregion //private methods
    }

    /**
     * ControlWheelSegment
     * 
     * This stores the data associated with a ControlWheel segment.
     * 
     */
    public class ControlWheelSegment
    {

        //Name of the segment. Used to find a segment if it needs to be removed at a later date.
        public string Name { get; private set; }

        //The Action to be performed when called by Select
        public Action Action { get; private set; }

        //The icon for the segment. A useful glyph is most appropriate for this.
        public Sprite Icon { get; private set; }

        //Will a label be created and displayed?
        public bool ShowLabel { get; private set; }

        //PreferredPostion Enum
        public enum PreferredPosition
        {
            None,
            Top,
            Left,
            Right,
            Bottom
        }

        //This ControlWheel segment's preferred position
        PreferredPosition preferredPosition;

        /**
         * PreferredIndex
         * Returns a preferred index based on this segment's preferred position and the number of segments of the ControlWheel.
         * This index is used to place the segment in the right place, as best the ControlWheel can fit it. See ControlWheel for
         * more on preferred position.
         */
        public int PreferredIndex(int numSegs)
        {

            switch (this.preferredPosition)
            {

                case PreferredPosition.None:
                    return -1;

                case PreferredPosition.Top:
                    return 0;
                case PreferredPosition.Bottom:
                    if (numSegs > 1)
                    {
                        return numSegs / 2;
                    }
                    else
                    {
                        return -1;
                    }

                case PreferredPosition.Left:
                    if (numSegs > 3)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                case PreferredPosition.Right:
                    if (numSegs > 3)
                    {
                        return numSegs - 1;
                    }
                    else
                    {
                        return -1;
                    }
            }

            return -1;
        }


        /**
         * ControlWheelSegment - Constructor
         * Takes a name (can be null), an Action (should not be null), a Sprite icon (can be null) and an optional PreferredPosition
         */
        public ControlWheelSegment(string name, Action action, Sprite icon, PreferredPosition preferredPosition = PreferredPosition.None, bool showLabel = false)
        {
            this.Name = name;
            this.Action = action;
            this.Icon = icon;
            this.preferredPosition = preferredPosition;
            this.ShowLabel = showLabel;
        }
    }
}