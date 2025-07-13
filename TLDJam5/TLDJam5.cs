using System.Linq;
using System.Reflection;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.Playables;
using NewHorizons.Components.SizeControllers;

namespace TLDJam5
{
    public class TLDJam5 : ModBehaviour
    {
        public static TLDJam5 Instance;
        public INewHorizons NewHorizons;
        public ShrinkingPlanetControler shrinkingPlanetControler;

        public string planetBodyPath = "TheLoweDown256_ShrinkingPlanet_Body";
        public string sunBodyPath = "HiddenLight_Body";

        public OWRigidbody planetRigidbody;

        public bool isSunCloakingActive;
        public GameObject sunCloakingObject;
        public float cloakDisableRadius = 100;

        public int startDelay = 10;

        public NomaiInterfaceOrb[] towerOrbs = [null,null];
        public int[] towerOrbStayTimes = [0, 0];
        public bool towerOrbsSolved = false;
        public int towerOrbSetStayTime = 20 * 60;

        public int[] treeSpots = [0,1,2,3,4,6,7,8,9,11,12,13,14,16,17,18,19]; 

        public bool inJam5System = false;

        public float roofOpen = 0f;
        public float roofOpenTarg = 0f;
        public float roofOpenSpeed = 0.001f;
        public Transform roofPivot;
        public Transform[] roofLocations = [null,null];

        public StarEvolutionController miniSunEvolutionControler;
        public float miniSunSize=40;

        public void Awake()
        {
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        public void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(TLDJam5)} is loaded!", MessageType.Success);

            // Get the New Horizons API and load configs
            NewHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            NewHorizons.LoadConfigs(this);

            new Harmony("TheLoweDown256.TLDJam5").PatchAll(Assembly.GetExecutingAssembly());

            // Example of accessing game code.
            //OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
            //LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
            NewHorizons.GetStarSystemLoadedEvent().AddListener(OnStarSystemLoaded);
        }

        /*public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            if (newScene != OWScene.SolarSystem) return;
            ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
        }*/

        public void OnStarSystemLoaded(string systemName)
        {
            inJam5System = systemName == "Jam5";
            shrinkingPlanetControler = null;
            if (inJam5System)
            {
                startDelay = 10;
                isSunCloakingActive = true;
                sunCloakingObject = GameObject.Find(sunBodyPath + "/Sector/CloakingField");
                postNHInit();          
            }

        }

        public void postNHInit()
        {
            GameObject planetBody = GameObject.Find(planetBodyPath);
            planetRigidbody=planetBody.GetAttachedOWRigidbody();
            // ModHelper.Console.WriteLine("is this null plase not be nul: " + planetBody, MessageType.Info);
            shrinkingPlanetControler = planetBody.GetAddComponent<ShrinkingPlanetControler>();
            shrinkingPlanetControler.transformsToScale.Add(planetBody.transform.Find("Sector"));
            shrinkingPlanetControler.transformsToScale.Add(planetBody.transform.Find("Volumes"));
            // ModHelper.Console.WriteLine("is this null plase not be nul: " + shrinkingPlanetControler.transformToScale, MessageType.Info);
            makeSolarPanelTrees();

            GameObject.Find(planetBodyPath + "/Sector/Structure_NOM_WarpReceiver_CaveTwin_Copper/Socket").transform.localScale *= 2;

            towerOrbsSolved = false;
            towerOrbStayTimes = [0, 0];
            towerOrbs[0] = GameObject.Find(planetBodyPath + "/Sector/TowerPuzzle/EastOrbInterface/Prefab_NOM_InterfaceOrb").GetComponent<NomaiInterfaceOrb>();
            towerOrbs[1] = GameObject.Find(planetBodyPath + "/Sector/TowerPuzzle/WestOrbInterface/Prefab_NOM_InterfaceOrb").GetComponent<NomaiInterfaceOrb>();
            towerOrbs[0]._occupiedSlot = towerOrbs[0]._slots[0];
            towerOrbs[1]._occupiedSlot = towerOrbs[1]._slots[0];

            roofPivot = GameObject.Find(planetBodyPath + "/Sector/shrinkingplanet/planet/roof/roof_pivot").transform;
            roofLocations[0] = GameObject.Find(planetBodyPath + "/Sector/shrinkingplanet/planet/roof/pivot_closed").transform;
            roofLocations[1] = GameObject.Find(planetBodyPath + "/Sector/shrinkingplanet/planet/roof/pivot_open").transform;

            for(int i = 0; i < 5; i++)
            {
                GameObject light= GameObject.Find(planetBodyPath + "/Sector/shrinkingplanet/light/inLight"+i);
                shrinkingPlanetControler.lights.Add(light.GetComponent<Light>());
            }

            Transform tempTr;
            tempTr = GameObject.Find(planetBodyPath + "/Sector/TowerPuzzle/EastOrbInterface/Prefab_NOM_InterfaceOrb").transform;
            shrinkingPlanetControler.transformsToScale.Add(tempTr);
            shrinkingPlanetControler.baseScales.Add(tempTr, 3);
            tempTr= GameObject.Find(planetBodyPath + "/Sector/TowerPuzzle/WestOrbInterface/Prefab_NOM_InterfaceOrb").transform;
            shrinkingPlanetControler.transformsToScale.Add(tempTr);
            shrinkingPlanetControler.baseScales.Add(tempTr, 3);

            tempTr= GameObject.Find(planetBodyPath + "/Sector/Prefab_NOM_WarpTransmitter").transform;
            tempTr.Find("Props_NOM_WarpCoreBlack").gameObject.SetActive(false);
            tempTr.Find("PointLight_NOM_WarpCoreBlack (1)").gameObject.SetActive(false);
            tempTr.Find("Structure_NOM_WarpTransmitter/Structure_NOM_WarpTransmitter_Effects").gameObject.SetActive(false);

            miniSunSize = 40;
            miniSunEvolutionControler= GameObject.Find(sunBodyPath + "/Sector/Star").GetComponent<StarEvolutionController>();


        }

        public void makeSolarPanelTrees()
        {
            Transform baseTree = GameObject.Find(planetBodyPath + "/Sector/solarpaneltrees/paneltree").transform;
            MeshRenderer btMeshRenderer=baseTree.transform.Find("GEO_NomaiTree_1_Trunk").GetComponent<MeshRenderer>();
            ModHelper.Console.WriteLine("barkmeshrenderer " + btMeshRenderer, MessageType.Info);
            GameObject atpCables = GameObject.Find(planetBodyPath+ "/Sector/ControlledByProxy_TimeLoopInterior/PowerCables_Interior_Hidden");//"TowerTwin_Body/Sector_TowerTwin/Sector_TimeLoopInterior/Geometry_TimeLoopInterior/ControlledByProxy_TimeLoopInterior/PowerCables_Interior_Hidden");
            ModHelper.Console.WriteLine("atpcables " + atpCables, MessageType.Info);
            Material cableMat = atpCables.GetComponent<MeshRenderer>().materials[0];
            ModHelper.Console.WriteLine("cablemat " + cableMat, MessageType.Info);
            ModHelper.Console.WriteLine("barkmat " + btMeshRenderer.materials[0], MessageType.Info);
            btMeshRenderer.material= cableMat;


            Transform rootTransform=GameObject.Find(planetBodyPath + "/Sector/solarpaneltrees").transform;

            for (int i = 0; i < treeSpots.Count(); i++)
            {
                Transform newTree=Instantiate(baseTree).transform;
                newTree.parent=rootTransform;
                Quaternion rot = Quaternion.Euler(0f, 18f * treeSpots[i], 0f);
                newTree.localPosition = rot * baseTree.localPosition;
                newTree.localRotation = rot * baseTree.localRotation;
                CapsuleCollider cCol=newTree.gameObject.GetAddComponent<CapsuleCollider>();
                cCol.radius = 0.6f;
                cCol.height = 3f;
                cCol.center = new Vector3(0, 1.5f, 0);
            }
            for (int j = -1; j < 2; j += 2) {
                for (int i = 0; i < 20; i++)
                {
                    Transform newTree = Instantiate(baseTree).transform;
                    newTree.parent = rootTransform;
                    Quaternion rot = Quaternion.Euler(18f* j* 0.866f, 18f * i+9, 0f);
                    newTree.localPosition = rot * baseTree.localPosition;
                    newTree.localRotation = rot * baseTree.localRotation;
                    CapsuleCollider cCol = newTree.gameObject.GetAddComponent<CapsuleCollider>();
                    cCol.radius = 0.6f;
                    cCol.height = 3f;
                    cCol.center = new Vector3(0, 1.5f, 0);
                }
            }

            Destroy(baseTree.gameObject);
        }

        public void FixedUpdate()
        {
            if (inJam5System)
            {
                if (startDelay!=-1)
                {
                    if (startDelay == 0)
                    {
                        
                    }
                    startDelay--;
                }
                sunCloakUpdate();
                if (!shrinkingPlanetControler.planetGone)
                {
                    towerOrbsUpdate();
                    if (roofOpen!=roofOpenTarg)
                    {
                        roofOpenUpdate();
                    }
                }
                else
                {
                    sunSizeUpdate();
                }

                scoutHelperDebugDeactivate();

            }
        }


        public void sunSizeUpdate()
        {
            if (miniSunEvolutionControler.size > 20f)
            {
                miniSunEvolutionControler.size -= 0.1f;
            }
            
        }



        public void sunCloakUpdate()
        {
            if (Locator.GetPlayerBody() == null || GameObject.Find(sunBodyPath).GetAttachedOWRigidbody() == null) { return; }
            float pDist = (Locator.GetPlayerBody().GetPosition() - GameObject.Find(sunBodyPath).GetAttachedOWRigidbody().GetPosition()).magnitude;
            if (isSunCloakingActive)
            {
                if(pDist< cloakDisableRadius)
                {
                    sunCloakingObject.SetActive(false);
                    isSunCloakingActive=false;
                    ModHelper.Console.WriteLine("MADE MINISUN INACTIVE", MessageType.Info);
                }
            }
            else
            {
                if (pDist > cloakDisableRadius+5)
                {
                    sunCloakingObject.SetActive(true);
                    isSunCloakingActive = true;
                    ModHelper.Console.WriteLine("MADE MINISUN ACTIVE", MessageType.Info);
                }
            }
        }


        public void roofOpenUpdate()
        {
            if (roofOpen<roofOpenTarg)
            {
                roofOpen += roofOpenSpeed;
            }
            if (roofOpen > roofOpenTarg)
            {
                roofOpen -= roofOpenSpeed;
            }
            roofOpen = Mathf.Clamp01(roofOpen);

            roofPivot.localPosition = Vector3.Lerp(roofLocations[0].localPosition, roofLocations[1].localPosition, roofOpen);
            roofPivot.localRotation = Quaternion.Lerp(roofLocations[0].localRotation, roofLocations[1].localRotation, roofOpen);


        }



        public void towerOrbsUpdate()
        {
            if (towerOrbsSolved)
            {
                int orbsInUp = 0;
                for (int i = 0; i < towerOrbs.Count(); i++)
                {
                    if (towerOrbs[i]._occupiedSlot == towerOrbs[i]._slots[1])
                    {
                        orbsInUp++;
                    }
                }
                if (orbsInUp < towerOrbs.Count())
                {
                    for (int i = 0; i < towerOrbs.Count(); i++)
                    {
                        towerOrbs[i]._glowBaseColor = new UnityEngine.Color(0.0039f, 0.0037f, 0.01f, 1f);
                        towerOrbs[i]._occupiedSlot = towerOrbs[i]._slots[0];
                    }
                    towerOrbsSolved = false;
                    roofOpenTarg = 0;
                }
            }
            else
            {
                int orbsInUp = 0;
                for (int i = 0; i < towerOrbs.Count(); i++)
                {
                    if (towerOrbStayTimes[i] > 0)
                    {
                        towerOrbStayTimes[i]--;
                        if (towerOrbStayTimes[i] <= 0)
                        {
                            towerOrbs[i]._occupiedSlot = towerOrbs[i]._slots[0];
                        }
                    }
                    else
                    {
                        if (towerOrbs[i]._occupiedSlot == towerOrbs[i]._slots[1])
                        {
                            towerOrbStayTimes[i] = towerOrbSetStayTime;
                        }
                    }
                    if (towerOrbs[i]._occupiedSlot == towerOrbs[i]._slots[1])
                    {
                        orbsInUp++;
                    }

                }
                if (orbsInUp == towerOrbs.Count())
                {
                    towerOrbsSolved = true;
                    for (int i = 0; i < towerOrbs.Count(); i++)
                    {
                        towerOrbs[i]._glowBaseColor = new UnityEngine.Color(0,1,0, 1f);
                    }
                    roofOpenTarg = 1;
                }
            }
        }





        public bool doesShrinkingPlanetExist()
        {
            return inJam5System && !shrinkingPlanetControler.planetGone;
        }

        public bool isPlayerAroundShrinkingPlanet()
        {
            if (doesShrinkingPlanetExist())
            {
                float pDist = (Locator.GetPlayerBody().GetPosition() - TLDJam5.Instance.planetRigidbody.GetPosition()).magnitude;
                if (pDist < 300f * TLDJam5.Instance.shrinkingPlanetControler.curentScale)
                {
                    return true;
                }
            }
            return false;
        }


        public void scoutHelperDebugDeactivate()
        {
            if (OWInput.IsNewlyPressed(InputLibrary.autopilot, InputMode.Character))
            {
                Locator.GetProbe().transform.parent.gameObject.SetActive(false);
            }
            
        }
    }



}
