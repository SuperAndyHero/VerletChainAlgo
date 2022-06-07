using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VerletChainAlgo
{
    public class VerletChainInstance
    {
        //basic variables
        public bool Active = true;

        public int segmentCount;
        public float segmentDistance;

        public int constraintRepetitions = 2;
        public float drag = 1f;
        public Vector2 forceGravity = Vector2.Zero;

        //medium variables
        public bool useStartPoint = true;
        public Vector2 startPoint = Vector2.Zero;
        public bool useEndPoint = false;
        public Vector2 endPoint = Vector2.Zero;

        //advanced variables
        public bool customDistances = false;
        public List<float> segmentDistances;//length must match the segment count

        public bool customGravity = false;
        public List<Vector2> forceGravities;//length must match the segment count



        public List<RopeSegment> ropeSegments;

        public VerletChainInstance(int SegCount, bool specialDraw, float SegDistance)
        {
            segmentCount = SegCount;
            segmentDistance = SegDistance;

            Start();
        }

        public VerletChainInstance(int SegCount, Vector2? StartPoint = null, Vector2? EndPoint = null, float SegDistance = 5, Vector2? Grav = null)
        {
            segmentCount = SegCount;
            segmentDistance = SegDistance;

            forceGravity = Grav ?? Vector2.Zero;

            startPoint = StartPoint ?? Vector2.Zero;

            endPoint = EndPoint ?? Vector2.Zero;

            Start(EndPoint != null);
        }

        public VerletChainInstance(int SegCount, Vector2? StartPoint = null, Vector2? EndPoint = null, float SegDistance = 5, Vector2? Grav = null,
            bool CustomGravs = false, List<Vector2> SegGravs = null,
            bool CustomDists = false, List<float> SegDists = null)
        {
            segmentCount = SegCount;
            segmentDistance = SegDistance;

            forceGravity = Grav ?? (CustomGravs ? Vector2.One : Vector2.Zero);

            startPoint = StartPoint ?? Vector2.Zero;

            endPoint = EndPoint ?? Vector2.Zero;

            if (customGravity = CustomGravs)
                forceGravities = SegGravs ?? Enumerable.Repeat(forceGravity, segmentCount).ToList();

            if (customDistances = CustomDists)
                segmentDistances = SegDists ?? Enumerable.Repeat(segmentDistance, segmentCount).ToList();

            Start(EndPoint != null);
        }

        public void Start(bool SpawnEndPoint = false)//public in case you want to reset the chain
        {
            ropeSegments = new List<RopeSegment>();

            Vector2 nextRopePoint = startPoint;

            for (int i = 0; i < segmentCount; i++)
            {
                ropeSegments.Add(new RopeSegment(nextRopePoint));

                if (SpawnEndPoint)
                {
                    float distance = (int)(Vector2.Distance(startPoint, endPoint) / segmentCount);
                    nextRopePoint += Vector2.Normalize(endPoint - nextRopePoint) * distance * i;
                }
                else
                {
                    float distance = customDistances ? segmentDistances[i] : segmentDistance;
                    Vector2 spawnGrav = customGravity ? forceGravities[i] * forceGravity : forceGravity;
                    if (spawnGrav != Vector2.Zero)
                        nextRopePoint += Vector2.Normalize(spawnGrav) * distance;
                    else
                        nextRopePoint.Y += distance;
                }
            }
        }

        public void UpdateChain(Vector2 Start, Vector2 End)
        {
            endPoint = End;
            startPoint = Start;
            UpdateChain();
        }

        public void UpdateChain(Vector2 Start)
        {
            startPoint = Start;
            UpdateChain();
        }

        public void UpdateChain()
        {
            if (Active)
                Simulate();
        }

        private void Simulate()
        {
            for (int i = 0; i < segmentCount; i++)
            {
                RopeSegment segment = ropeSegments[i];
                Vector2 velocity = (segment.posNow - segment.posOld) / drag;
                segment.posOld = segment.posNow;
                segment.posNow += velocity;
                segment.posNow += customGravity ? forceGravities[i] * forceGravity : forceGravity;
            }

            for (int i = 0; i < constraintRepetitions; i++)//the amount of times Constraints are applied per update
            {
                if (useStartPoint)
                    ropeSegments[0].posNow = startPoint;
                if (useEndPoint)
                    ropeSegments[segmentCount - 1].posNow = endPoint;
                ApplyConstraint();
            }
        }

        private void ApplyConstraint()
        {
            for (int i = 0; i < segmentCount - 1; i++)
            {
                float segmentDist = customDistances ? segmentDistances[i] : segmentDistance;

                float dist = (ropeSegments[i].posNow - ropeSegments[i + 1].posNow).Length();
                float error = Math.Abs(dist - segmentDist);
                Vector2 changeDir = Vector2.Zero;

                if (dist > segmentDist)
                    changeDir = Vector2.Normalize(ropeSegments[i].posNow - ropeSegments[i + 1].posNow);
                else if (dist < segmentDist)
                    changeDir = Vector2.Normalize(ropeSegments[i + 1].posNow - ropeSegments[i].posNow);

                Vector2 changeAmount = changeDir * error;
                if (i != 0)
                {
                    ropeSegments[i].posNow -= changeAmount * 0.5f;
                    ropeSegments[i] = ropeSegments[i];
                    ropeSegments[i + 1].posNow += changeAmount * 0.5f;
                    ropeSegments[i + 1] = ropeSegments[i + 1];
                }
                else
                {
                    ropeSegments[i + 1].posNow += changeAmount;
                    ropeSegments[i + 1] = ropeSegments[i + 1];
                }
            }
        }
    }

    public class RopeSegment
    {
        public Vector2 posNow;
        public Vector2 posOld;

        //public Vector2 ScreenPos => posNow - Main.screenPosition;

        public RopeSegment(Vector2 pos)
        {
            posNow = pos;
            posOld = pos;
        }
    }
}