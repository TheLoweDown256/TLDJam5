using System.Linq;
using System.Reflection;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.Playables;
using NewHorizons.Components.SizeControllers;
using System.Security.Policy;
using NewHorizons.Components.Props;
using System.Collections.Generic;

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

        //public int startDelay = 10;

        public NomaiInterfaceOrb[] towerOrbs = [null,null];
        public int[] towerOrbStayTimes = [0, 0];
        public bool towerOrbsSolved = false;
        public int towerOrbSetStayTime = 1000;// <- a bit over 15 sec      //25 * 60;

        public bool towerRoofsOpen = false;
        public NomaiInterfaceOrb towerOpenSwitch;

        public int[] treeSpots = [0,1,2,3,4,6,7,8,9,11,12,13,14,16,17,18,19]; 

        public bool inJam5System = false;

        public float roofOpen = 0f;
        public float roofOpenTarg = 0f;
        public float roofOpenSpeed = 0.001f;
        public Transform roofPivot;
        public Transform[] roofLocations = [null,null];

        public StarEvolutionController miniSunEvolutionControler;
        public float miniSunSize=40;

        public GameObject whiteWarpCore;
        public float whiteWarpCoreScale = 2;
        ItemTool playerItemCarryTool;

        public Transform shrinkingPlanetSector;

        public GameObject[] towerClosedRoofs = [null, null];
        public GameObject[] towerOpenRoofs = [null, null];

        public NHItemSocket whiteWarpCoreSocket;

        public List<NomaiComputer> nComputers = new();

        public bool isTheMainCoreFixed=false;

        public bool hasDoneComputerInit;

        public NomaiComputer warpSComputer;
        public NomaiInterfaceOrb warpSOrb;

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
                //startDelay = 10;
                isSunCloakingActive = true;
                sunCloakingObject = GameObject.Find(sunBodyPath + "/Sector/CloakingField");
                postNHInit();          
            }

        }

        public void postNHInit()
        {
            GameObject planetBody = GameObject.Find(planetBodyPath);
            planetRigidbody=planetBody.GetAttachedOWRigidbody();
            shrinkingPlanetSector = GameObject.Find(planetBodyPath + "/Sector").transform;
            // ModHelper.Console.WriteLine("is this null plase not be nul: " + planetBody, MessageType.Info);
            shrinkingPlanetControler = planetBody.GetAddComponent<ShrinkingPlanetControler>();
            shrinkingPlanetControler.transformsToScale.Add(planetBody.transform.Find("Sector"));
            shrinkingPlanetControler.transformsToScale.Add(planetBody.transform.Find("Volumes"));
            // ModHelper.Console.WriteLine("is this null plase not be nul: " + shrinkingPlanetControler.transformToScale, MessageType.Info);
            makeSolarPanelTrees();

            NomaiComputer temp3 = GameObject.Find(sunBodyPath + "/Sector/sunComputer").GetComponent<NomaiComputer>();
            shrinkingPlanetControler.sunComputer = temp3;
            

            GameObject.Find(planetBodyPath + "/Sector/Structure_NOM_WarpReceiver_CaveTwin_Copper/Socket").transform.localScale *= 2;

            towerOrbsSolved = false;
            towerOrbStayTimes = [0, 0];
            towerOrbs[0] = GameObject.Find(planetBodyPath + "/Sector/TowerPuzzle/EastOrbInterface/Prefab_NOM_InterfaceOrb").GetComponent<NomaiInterfaceOrb>();
            towerOrbs[1] = GameObject.Find(planetBodyPath + "/Sector/TowerPuzzle/WestOrbInterface/Prefab_NOM_InterfaceOrb").GetComponent<NomaiInterfaceOrb>();
            towerOrbs[0]._occupiedSlot = towerOrbs[0]._slots[0];
            towerOrbs[1]._occupiedSlot = towerOrbs[1]._slots[0];

            hasDoneComputerInit = false;

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
            shrinkingPlanetControler.baseScales.Add(tempTr, 2.5f);
            tempTr= GameObject.Find(planetBodyPath + "/Sector/TowerPuzzle/WestOrbInterface/Prefab_NOM_InterfaceOrb").transform;
            shrinkingPlanetControler.transformsToScale.Add(tempTr);
            shrinkingPlanetControler.baseScales.Add(tempTr, 2.5f);

            tempTr = GameObject.Find(planetBodyPath + "/Sector/WarpStabilizer/Prefab_NOM_InterfaceOrb").transform;
            shrinkingPlanetControler.transformsToScale.Add(tempTr);
            shrinkingPlanetControler.baseScales.Add(tempTr, 2f);

            warpSOrb = tempTr.GetComponent<NomaiInterfaceOrb>();
            warpSOrb._occupiedSlot = warpSOrb._slots[0];


            tempTr = GameObject.Find(planetBodyPath + "/Sector/Prefab_NOM_WarpTransmitter").transform;
            tempTr.Find("Props_NOM_WarpCoreBlack").gameObject.SetActive(false);
            tempTr.Find("PointLight_NOM_WarpCoreBlack (1)").gameObject.SetActive(false);
            tempTr.Find("Structure_NOM_WarpTransmitter/Structure_NOM_WarpTransmitter_Effects").gameObject.SetActive(false);

            tempTr = GameObject.Find(sunBodyPath + "/Sector/Prefab_NOM_WarpReceiver").transform;
            tempTr.Find("PointLight_NOM_WarpCoreWhite (1)").gameObject.SetActive(false);
            tempTr.Find("Props_NOM_WarpCoreWhite").gameObject.SetActive(false);
            tempTr.Find("Structure_NOM_WarpReceiver_Effects").gameObject.SetActive(false);
            tempTr.Find("PointLight_TH_WarpReceiver").gameObject.SetActive(false);
            tempTr.Find("Effects_NOM_ReverseWarpParticles").gameObject.SetActive(false);

            miniSunSize = 40;
            miniSunEvolutionControler= GameObject.Find(sunBodyPath + "/Sector/Star").GetComponent<StarEvolutionController>();

            GameObject.Find(planetBodyPath + "/Sector/Atmosphere").SetActive(false);

            


            whiteWarpCore = GameObject.Find(planetBodyPath + "/Sector/ShrinkingPlanet_WarpCoreWhite");
            whiteWarpCoreScale = 2;
            if (whiteWarpCore == null)
            {
                ModHelper.Console.WriteLine("[logged error] WHITEHOLEWARPCORE is NULL ", MessageType.Error);
            }

            isTheMainCoreFixed = false;

            towerClosedRoofs[0] = GameObject.Find(planetBodyPath + "/Sector/shrinkingplanet/planet/towers/tower_east/roof/closed");
            towerClosedRoofs[1] = GameObject.Find(planetBodyPath + "/Sector/shrinkingplanet/planet/towers/tower_west/roof/closed");
            towerOpenRoofs[0] = GameObject.Find(planetBodyPath + "/Sector/shrinkingplanet/planet/towers/tower_east/roof/open");
            towerOpenRoofs[1] = GameObject.Find(planetBodyPath + "/Sector/shrinkingplanet/planet/towers/tower_west/roof/open");

            towerOpenRoofs[0].SetActive(false);
            towerOpenRoofs[1].SetActive(false);
            towerRoofsOpen = false;

            towerOpenSwitch = GameObject.Find(planetBodyPath + "/Sector/OpenTowerSwRoot/OpenTowerSwitch/Prefab_NOM_InterfaceOrb").GetComponent<NomaiInterfaceOrb>();
            towerOpenSwitch._occupiedSlot = towerOpenSwitch._slots[0];

            shrinkingPlanetControler.transformsToScale.Add(towerOpenSwitch.transform);
            shrinkingPlanetControler.baseScales.Add(towerOpenSwitch.transform, 2f);

            warpSComputer = GameObject.Find(planetBodyPath + "/Sector/WarpSComputer").GetComponent<NomaiComputer>();


            tempTr = GameObject.Find(planetBodyPath + "/Sector/Prefab_GravityCannon/NomaiInterfaceOrb_Body").transform;
            shrinkingPlanetControler.transformsToScale.Add(tempTr);
            shrinkingPlanetControler.baseScales.Add(tempTr, 1f);

            whiteWarpCoreSocket = GameObject.Find(planetBodyPath + "/Sector/Structure_NOM_WarpReceiver_CaveTwin_Copper").GetComponent<NHItemSocket>();

            tempTr = GameObject.Find(planetBodyPath + "/Sector/NomaiComputers").transform;

            nComputers = new();
            for(int i = 0; i < tempTr.childCount; i++)
            {
                NomaiComputer com=tempTr.GetChild(i).GetComponent<NomaiComputer>();
                if (com != null)
                {
                    nComputers.Add(com);
                    
                }
            }

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
               // if (startDelay!=-1)
                //{
                 //   if (startDelay == 0)
                  //  {
                        
                  //  }
                 //   startDelay--;
              //  }

                if (!hasDoneComputerInit)
                {
                    if ((Locator.GetPlayerBody().GetPosition() - TLDJam5.Instance.planetRigidbody.GetPosition()).magnitude < 1000f)
                    {
                        shrinkingPlanetControler.sunComputer.ClearEntry(3);
                        for (int i = 0; i < nComputers.Count; i++)
                        {
                            NomaiComputer com=nComputers[i];
                            for (int j = 3; j <= com.GetNumTextBlocks(); j++)
                            {
                                com.ClearEntry(j);
                            }
                        }
                        warpSComputer.ClearAllEntries();
                        hasDoneComputerInit = true;
                    }
                }
                sunCloakUpdate();
                if (!shrinkingPlanetControler.planetGone)
                {
                    towerOrbsUpdate();
                    towersOpenUpdate();
                    if (roofOpen!=roofOpenTarg)
                    {
                        roofOpenUpdate();
                    }

                    if (!isTheMainCoreFixed)
                    {
                        warpCoreSizeUpdate();
                    }
                    
                }
                else
                {
                    sunSizeUpdate();
                }

                scoutHelperDebugDeactivate();

                if (playerItemCarryTool == null)
                {
                    var temp2 = Locator.GetPlayerController().transform.Find("PlayerCamera/ItemCarryTool");
                    ModHelper.Console.WriteLine("TEMP2: " + temp2, MessageType.Info);
                    if (temp2 == null)
                    {
                        ModHelper.Console.WriteLine("[logged error] temp2 is NULL ", MessageType.Error);
                    }
                    playerItemCarryTool = temp2.GetComponent<ItemTool>();


                    if (playerItemCarryTool == null)
                    {
                        ModHelper.Console.WriteLine("[logged error] playerItemCarryTool is NULL ", MessageType.Error);
                    }
                }

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



        public void towersOpenUpdate()
        {
            if (towerRoofsOpen)
            {
                if (towerOpenSwitch._occupiedSlot == towerOpenSwitch._slots[0])
                {
                    towerRoofsOpen = false;
                    towerOpenRoofs[0].SetActive(false);
                    towerOpenRoofs[1].SetActive(false);
                    towerClosedRoofs[0].SetActive(true);
                    towerClosedRoofs[1].SetActive(true);
                }
                    
            }
            else
            {
                if(towerOpenSwitch._occupiedSlot == towerOpenSwitch._slots[1])
                {
                    towerRoofsOpen = true;
                    towerOpenRoofs[0].SetActive(true);
                    towerOpenRoofs[1].SetActive(true);
                    towerClosedRoofs[0].SetActive(false);
                    towerClosedRoofs[1].SetActive(false);
                }
            }
           
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
                        towerOrbs[i]._glowBaseColor = new UnityEngine.Color(0.0039f, 0.0037f, 0.01f, 1f);//blues
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
                        if(towerOrbs[i]._occupiedSlot == towerOrbs[i]._slots[0])
                        {
                            towerOrbStayTimes[i] = 0;
                        }
                        if (towerOrbStayTimes[i] <= 0)
                        {
                            towerOrbs[i]._occupiedSlot = towerOrbs[i]._slots[0];
                            towerOrbs[i]._glowBaseColor =new UnityEngine.Color(0.0039f, 0.0037f, 0.01f, 1f);//blue
                        }
                    }
                    else
                    {
                        if (towerOrbs[i]._occupiedSlot == towerOrbs[i]._slots[1])
                        {
                            towerOrbStayTimes[i] = towerOrbSetStayTime;
                            towerOrbs[i]._glowBaseColor = new UnityEngine.Color(1f, 0.5f, 0.01f, 1f);//orange
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
                        towerOrbs[i]._glowBaseColor = new UnityEngine.Color(0,1f,0, 1f);//green
                    }
                    roofOpenTarg = 1;
                }
            }
        }


        public void warpCoreSizeUpdate()
        {
            
            bool isHeld = false;
            if (playerItemCarryTool != null)
            {
                if (playerItemCarryTool._heldItem != null)
                {
                    isHeld = whiteWarpCore.GetComponent<WarpCoreItem>() == playerItemCarryTool._heldItem;
                }
            }
            if (!isHeld)
            {
                Transform tryGetWarpCore = planetRigidbody.transform.Find("ShrinkingPlanet_WarpCoreWhite");
                if (tryGetWarpCore!=null)
                {
                    tryGetWarpCore.localScale= Vector3.one*whiteWarpCoreScale;
                    tryGetWarpCore.parent = shrinkingPlanetSector;
                }
                if (whiteWarpCore.transform.parent == shrinkingPlanetSector) {
                    whiteWarpCoreScale -= shrinkingPlanetControler.shrinkPerSecond /30f;
                }
            }

            if (whiteWarpCoreScale< shrinkingPlanetControler.curentScale)
            {
                whiteWarpCoreSocket.ItemType = ItemType.WarpCore;
                if (whiteWarpCoreSocket.GetSocketedItem() == whiteWarpCore)
                {
                    finalThingDone();
                }
            }
            else
            {
                whiteWarpCoreSocket.ItemType = ItemType.Invalid;
            }
            

        }

        public void finalThingDone()
        {
            shrinkingPlanetControler.sizeToAdd = 9999;
            whiteWarpCoreSocket.EnableInteraction(false);

            for (int i = 0; i < nComputers.Count; i++)
            {
                nComputers[i].ClearEntry(1);
                nComputers[i].ClearEntry(2);
                for (int j = 3; j <= nComputers[i].GetNumTextBlocks(); j++)
                {
                    nComputers[i].DisplayEntry(j);
                }
            }
            shrinkingPlanetControler.sunComputer.ClearAllEntries();
            isTheMainCoreFixed = true;
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
