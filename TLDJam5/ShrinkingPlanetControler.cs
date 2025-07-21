using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NAudio.Mixer;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

namespace TLDJam5
{
    public class ShrinkingPlanetControler : MonoBehaviour
    {
        public float endTime=60*20;
        public List<Transform> transformsToScale = new();
        public GravityVolume gravityVolume;
        public float surfaceGravityStart;
        public float surfaceGravityRadiusStart;
        public List<Transform> transformsToUnscale = new();
        public float shrinkPerSecond;
        public List<Light> lights=new();
        public float lightStartRange=160;

        public Dictionary<Transform,float> baseScales = new();

        public bool planetGone = false;

        public float curentScale = 1;

        public float sizeToAdd = 0;

        public NomaiComputer sunComputer;

        public int antiLagCycle = 0;

        public void Awake()
        {
        }

        public void Start()
        {
            gravityVolume=this.transform.Find("GravityWell").GetComponent<GravityVolume>();
            surfaceGravityStart = gravityVolume._surfaceAcceleration;
            surfaceGravityRadiusStart = gravityVolume._upperSurfaceRadius;

            Transform dontScaleRoot =  this.transform.Find("Sector/DontScale");
            for(int i = 0; i < dontScaleRoot.childCount; i++)
            {
                transformsToUnscale.Add(dontScaleRoot.GetChild(i));
            }
            transformsToUnscale.Add(this.transform.Find("Sector/Ring"));

            shrinkPerSecond = 1f / endTime;

            GlobalMessenger.AddListener("ExitRoastingMode", new Callback(this.onExitCampFire));
            GlobalMessenger.AddListener("StopSleepingAtCampfire", new Callback(this.onExitCampFire));
            GlobalMessenger<Campfire>.AddListener("EnterRoastingMode", new Callback<Campfire>(this.onEnterCampFire));
            GlobalMessenger<bool>.AddListener("StartSleepingAtCampfire", new Callback<bool>(this.onEnterCampFire));
        }

        public void FixedUpdate()
        {
            
            if (planetGone) { return; }

            antiLagCycle--;
            if (antiLagCycle < 0)
            {
                antiLagCycle = 5;
            }

            if (sizeToAdd > 0)
            {
                float change = shrinkPerSecond / 2.5f;
                curentScale = Mathf.Min(curentScale+change,1);
                sizeToAdd -= change;
                endTime += change/60f;
            }
            else
            {
                curentScale -= shrinkPerSecond / 60f; //Mathf.Max(1f-Mathf.Clamp01(Time.timeSinceLevelLoad / endTime),0.00001f);
                endTime -= 1f / 60f;
            }

            if (transformsToScale != null)
            {
                if (TLDJam5.Instance.playerIsAroundSP < 2000)
                {
                    for (int i = 0; i < transformsToScale.Count; i++)
                    {

                        float toScale = curentScale;
                        if (baseScales.ContainsKey(transformsToScale[i]))
                        {
                            toScale *= baseScales[transformsToScale[i]];
                        }
                        transformsToScale[i].localScale = Vector3.one * toScale;
                    }
                }else
                {
                    transformsToScale[0].localScale = Vector3.one * curentScale;
                }
            }

            if (antiLagCycle == 0)
            {
                for (int i = 0; i < transformsToUnscale.Count; i++)
                {
                    transformsToUnscale[i].localScale = Vector3.one / curentScale;
                }

                for (int i = 0; i < lights.Count; i++)
                {
                    lights[i].range = lightStartRange * curentScale;
                }
                if (gravityVolume != null)
                {
                    gravityVolume._surfaceAcceleration = Mathf.Max(surfaceGravityStart * (float)Math.Pow(curentScale, 1.5f) - 0.1f, 0);
                    //gravityVolume._upperSurfaceRadius = surfaceGravityRadiusStart * curentScale;
                }
                if (curentScale <= 2.2f / 200f)//0.02)
                {
                    Destroy(transformsToScale[0].gameObject);
                    planetGone = true;

                    sunComputer.ClearAllEntries();
                    sunComputer.DisplayEntry(3);
                }
            }

            
        }

        public void onEnterCampFire(bool isDreamFire)
        {
            onEnterCampFire();
        }
        public void onEnterCampFire(Campfire whoCares)
        {
            onEnterCampFire();
        }

        public void onEnterCampFire()
        {
            if (!TLDJam5.Instance.isPlayerAroundShrinkingPlanet())
            {
                return;
            }
            Transform player = Locator._playerTransform;
            PlayerAttachPoint attPt= player.parent.GetComponent<PlayerAttachPoint>();
            if (attPt != null)
            {
               // TLDJam5.Instance.ModHelper.Console.WriteLine("AttachPoint (Before): "+ attPt._attachOffset, MessageType.Info);
                attPt.SetAttachOffset(attPt._attachOffset /= ((curentScale*3f+1f)/4f));
               // TLDJam5.Instance.ModHelper.Console.WriteLine("AttachPoint (After): " + attPt._attachOffset, MessageType.Info);
            }
            else
            {
                TLDJam5.Instance.ModHelper.Console.WriteLine("players attach point is null >::(",MessageType.Error);
            }
        }
        public void onExitCampFire()
        {
            if (!TLDJam5.Instance.isPlayerAroundShrinkingPlanet())
            {
                return;
            }
            Transform player = Locator._playerTransform;
            player.localScale = Vector3.one;
        }
    }
}
