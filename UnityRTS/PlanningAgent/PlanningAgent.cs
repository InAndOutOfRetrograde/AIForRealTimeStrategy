using System.Collections.Generic;
using System.Linq;
using GameManager.EnumTypes;
using GameManager.GameElements;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////
/// This is the Moron Agent
/////////////////////////////////////////////////////////////////////////////

namespace GameManager
{
    ///<summary>Planning Agent is the over-head planner that decided where
    /// individual units go and what tasks they perform.  Low-level 
    /// AI is handled by other classes (like pathfinding).
    ///</summary> 
    public class PlanningAgent : Agent
    {
        private const int MAX_NBR_WORKERS = 20;

        #region Private Data

        ///////////////////////////////////////////////////////////////////////
        // Handy short-cuts for pulling all of the relevant data that you
        // might use for each decision.  Feel free to add your own.
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The enemy's agent number
        /// </summary>
        private int enemyAgentNbr { get; set; }

        /// <summary>
        /// My primary mine number
        /// </summary>
        private int mainMineNbr { get; set; }

        /// <summary>
        /// My primary base number
        /// </summary>
        private int mainBaseNbr { get; set; }

        /// <summary>
        /// List of all the mines on the map
        /// </summary>
        private List<int> mines { get; set; }

        /// <summary>
        /// List of all of my workers
        /// </summary>
        private List<int> myWorkers { get; set; }

        /// <summary>
        /// List of all of my soldiers
        /// </summary>
        private List<int> mySoldiers { get; set; }

        /// <summary>
        /// List of all of my archers
        /// </summary>
        private List<int> myArchers { get; set; }

        /// <summary>
        /// List of all of my bases
        /// </summary>
        private List<int> myBases { get; set; }

        /// <summary>
        /// List of all of my barracks
        /// </summary>
        private List<int> myBarracks { get; set; }

        /// <summary>
        /// List of all of my refineries
        /// </summary>
        private List<int> myRefineries { get; set; }

        /// <summary>
        /// List of the enemy's workers
        /// </summary>
        private List<int> enemyWorkers { get; set; }

        /// <summary>
        /// List of the enemy's soldiers
        /// </summary>
        private List<int> enemySoldiers { get; set; }

        /// <summary>
        /// List of enemy's archers
        /// </summary>
        private List<int> enemyArchers { get; set; }

        /// <summary>
        /// List of the enemy's bases
        /// </summary>
        private List<int> enemyBases { get; set; }

        /// <summary>
        /// List of the enemy's barracks
        /// </summary>
        private List<int> enemyBarracks { get; set; }

        /// <summary>
        /// List of the enemy's refineries
        /// </summary>
        private List<int> enemyRefineries { get; set; }

        /// <summary>
        /// List of the possible build positions for a 3x3 unit
        /// </summary>
        private List<Vector3Int> buildPositions { get; set; }

        /// <summary>
        /// Finds all of the possible build locations for a specific UnitType.
        /// Currently, all structures are 3x3, so these positions can be reused
        /// for all structures (Base, Barracks, Refinery)
        /// Run this once at the beginning of the game and have a list of
        /// locations that you can use to reduce later computation.  When you
        /// need a location for a build-site, simply pull one off of this list,
        /// determine if it is still buildable, determine if you want to use it
        /// (perhaps it is too far away or too close or not close enough to a mine),
        /// and then simply remove it from the list and build on it!
        /// This method is called from the Awake() method to run only once at the
        /// beginning of the game.
        /// </summary>
        /// <param name="unitType">the type of unit you want to build</param>
        public void FindProspectiveBuildPositions(UnitType unitType)
        {
            // For the entire map
            for (int i = 0; i < GameManager.Instance.MapSize.x; ++i)
            {
                for (int j = 0; j < GameManager.Instance.MapSize.y; ++j)
                {
                    // Construct a new point near gridPosition
                    Vector3Int testGridPosition = new Vector3Int(i, j, 0);

                    // Test if that position can be used to build the unit
                    if (Utility.IsValidGridLocation(testGridPosition)
                        && GameManager.Instance.IsBoundedAreaBuildable(unitType, testGridPosition))
                    {
                        // If this position is buildable, add it to the list
                        buildPositions.Add(testGridPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Build a building
        /// </summary>
        /// <param name="unitType"></param>
        public void BuildBuilding(UnitType unitType, Unit proximityUnit)
        {
            // For each worker
            foreach (int worker in myWorkers)
            {
                // Grab the unit we need for this function
                Unit unit = GameManager.Instance.GetUnit(worker);
                Vector3Int pos = proximityUnit.GridPosition;

                // Make sure this unit actually exists and we have enough gold
                if (unit != null && Gold >= Constants.COST[unitType])
                {
                    //gets each object in build position, get the closest space between a toBuild and proximityItem
                    float smallestDist = 9999f;
                    Vector3Int readyToBuild = new Vector3Int(0,0,0);
                    
                    foreach (Vector3Int toBuild in buildPositions)
                    {
                        if (GameManager.Instance.IsBoundedAreaBuildable(unitType, toBuild))
                        {
                            Vector3Int num = toBuild - pos;
                            float dist = num.sqrMagnitude;
                            if (dist < smallestDist)
                            {
                                smallestDist = dist;
                                readyToBuild = toBuild;
                            }
                        }
                    }
                    //build the closest please
                    Build(unit, readyToBuild, unitType);
                    return;
                }
            }
        }

        /// <summary>
        /// Attack the enemy
        /// </summary>
        /// <param name="myTroops"></param>
        public void AttackEnemy(List<int> myTroops)
        {
            // For each of my troops in this collection
            foreach (int troopNbr in myTroops)
            {
                // If this troop is idle, give him something to attack
                Unit troopUnit = GameManager.Instance.GetUnit(troopNbr);
                if (troopUnit.CurrentAction == UnitAction.IDLE)
                {
                    // If there are archers to attack
                    if (enemyArchers.Count > 0)
                    {
                        Attack(troopUnit, GameManager.Instance.GetUnit(enemyArchers[Random.Range(0, enemyArchers.Count)]));
                    }
                    // If there are soldiers to attack
                    else if (enemySoldiers.Count > 0)
                    {
                        Attack(troopUnit, GameManager.Instance.GetUnit(enemySoldiers[Random.Range(0, enemySoldiers.Count)]));
                    }
                    // If there are workers to attack
                    else if (enemyWorkers.Count > 0)
                    {
                        Attack(troopUnit, GameManager.Instance.GetUnit(enemyWorkers[Random.Range(0, enemyWorkers.Count)]));
                    }
                    // If there are bases to attack
                    else if (enemyBases.Count > 0)
                    {
                        Attack(troopUnit, GameManager.Instance.GetUnit(enemyBases[Random.Range(0, enemyBases.Count)]));
                    }
                    // If there are barracks to attack
                    else if (enemyBarracks.Count > 0)
                    {
                        Attack(troopUnit, GameManager.Instance.GetUnit(enemyBarracks[Random.Range(0, enemyBarracks.Count)]));
                    }
                    // If there are refineries to attack
                    else if (enemyRefineries.Count > 0)
                    {
                        Attack(troopUnit, GameManager.Instance.GetUnit(enemyRefineries[Random.Range(0, enemyRefineries.Count)]));
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called at the end of each round before remaining units are
        /// destroyed to allow the agent to observe the "win/loss" state
        /// </summary>
        public override void Learn()
        {
            Debug.Log("PlanningAgent::Learn");
        }

        /// <summary>
        /// Called before each match between two agents.  Matches have
        /// multiple rounds. 
        /// </summary>
        public override void InitializeMatch()
        {
	        Debug.Log("Moron's: " + AgentName);
            Debug.Log("PlanningAgent::InitializeMatch");
        }

        /// <summary>
        /// Called at the beginning of each round in a match.
        /// There are multiple rounds in a single match between two agents.
        /// </summary>
        public override void InitializeRound()
        {
            Debug.Log("PlanningAgent::InitializeRound");
            actualPlayerState = playerState.building;
            buildPositions = new List<Vector3Int>();

            FindProspectiveBuildPositions(UnitType.BASE);

            // Set the main mine and base to "non-existent"
            mainMineNbr = -1;
            mainBaseNbr = -1;

            // Initialize all of the unit lists
            mines = new List<int>();

            myWorkers = new List<int>();
            mySoldiers = new List<int>();
            myArchers = new List<int>();
            myBases = new List<int>();
            myBarracks = new List<int>();
            myRefineries = new List<int>();

            enemyWorkers = new List<int>();
            enemySoldiers = new List<int>();
            enemyArchers = new List<int>();
            enemyBases = new List<int>();
            enemyBarracks = new List<int>();
            enemyRefineries = new List<int>();
        }

        /// <summary>
        /// Updates the game state for the Agent - called once per frame for GameManager
        /// Pulls all of the agents from the game and identifies who they belong to
        /// </summary>
        public void UpdateGameState()
        {
            // Update the common resources
            mines = GameManager.Instance.GetUnitNbrsOfType(UnitType.MINE);

            // Update all of my unitNbrs
            myWorkers = GameManager.Instance.GetUnitNbrsOfType(UnitType.WORKER, AgentNbr);
            mySoldiers = GameManager.Instance.GetUnitNbrsOfType(UnitType.SOLDIER, AgentNbr);
            myArchers = GameManager.Instance.GetUnitNbrsOfType(UnitType.ARCHER, AgentNbr);
            myBarracks = GameManager.Instance.GetUnitNbrsOfType(UnitType.BARRACKS, AgentNbr);
            myBases = GameManager.Instance.GetUnitNbrsOfType(UnitType.BASE, AgentNbr);
            myRefineries = GameManager.Instance.GetUnitNbrsOfType(UnitType.REFINERY, AgentNbr);

            // Update the enemy agents & unitNbrs
            List<int> enemyAgentNbrs = GameManager.Instance.GetEnemyAgentNbrs(AgentNbr);
            if (enemyAgentNbrs.Any())
            {
                enemyAgentNbr = enemyAgentNbrs[0];
                enemyWorkers = GameManager.Instance.GetUnitNbrsOfType(UnitType.WORKER, enemyAgentNbr);
                enemySoldiers = GameManager.Instance.GetUnitNbrsOfType(UnitType.SOLDIER, enemyAgentNbr);
                enemyArchers = GameManager.Instance.GetUnitNbrsOfType(UnitType.ARCHER, enemyAgentNbr);
                enemyBarracks = GameManager.Instance.GetUnitNbrsOfType(UnitType.BARRACKS, enemyAgentNbr);
                enemyBases = GameManager.Instance.GetUnitNbrsOfType(UnitType.BASE, enemyAgentNbr);
                enemyRefineries = GameManager.Instance.GetUnitNbrsOfType(UnitType.REFINERY, enemyAgentNbr);
            }
        }

        /// <summary>
        /// Update the GameManager - called once per frame
        /// </summary>
        enum playerState
        {
            building,
            setup,
            attack,
            winning
        }
        Vector3Int target;
        playerState actualPlayerState = playerState.building;
        public void WorkerTrainer()
        {
            // For each base, determine if it should train a worker
            foreach (int baseNbr in myBases)
            {
                // Get the base unit
                Unit baseUnit = GameManager.Instance.GetUnit(baseNbr);

                // If the base exists, is idle, we need a worker, and we have gold
                if (baseUnit != null && baseUnit.IsBuilt
                                     && baseUnit.CurrentAction == UnitAction.IDLE
                                     && Gold >= Constants.COST[UnitType.WORKER]
                                     && myWorkers.Count < MAX_NBR_WORKERS)
                {
                    Train(baseUnit, UnitType.WORKER);
                }
            }
        }
        public void ArmyTrainer()
        {
            if(myBarracks.Count > 1)
            {
                Unit barracksUnitSoldier = GameManager.Instance.GetUnit(myBarracks[1]);
                // If this barracks still exists, is idle, we need soldiers, and have gold
                if (barracksUnitSoldier != null && barracksUnitSoldier.IsBuilt
                    && barracksUnitSoldier.CurrentAction == UnitAction.IDLE
                    && Gold >= Constants.COST[UnitType.SOLDIER])
                {
                    Train(barracksUnitSoldier, UnitType.SOLDIER);
                }
            }
            Unit barracksUnitArcher = GameManager.Instance.GetUnit(myBarracks[0]);
            // If this barracks still exists, is idle, we need archers, and have gold
            if (barracksUnitArcher != null && barracksUnitArcher.IsBuilt
                        && barracksUnitArcher.CurrentAction == UnitAction.IDLE
                        && Gold >= Constants.COST[UnitType.ARCHER])
            {
                Train(barracksUnitArcher, UnitType.ARCHER);
            }
        }

        public void ArmyRally(List<int> units, Vector3Int loc)
        {
            //rally troops here pls
            foreach (int unit in units)
            {
                Unit currUnit = GameManager.Instance.GetUnit(unit);
                // Make sure this unit actually exists and is idle
                if (currUnit != null && currUnit.CurrentAction == UnitAction.IDLE && mainBaseNbr >= 0 && mainMineNbr >= 0)
                {
                    if (loc == new Vector3Int(0, 0, 0))
                    {
                        List<int> OneGuy = new List<int>{ unit };
                        AttackEnemy(OneGuy);
                    }
                    else
                    {
                        Move(currUnit, loc);
                    }
                }
                foreach(int archer in enemyArchers)
                {
                    Unit currEnemy = GameManager.Instance.GetUnit(archer);
                    if ((currEnemy.GridPosition - currUnit.GridPosition).sqrMagnitude < Mathf.Pow(Constants.ATTACK_RANGE[UnitType.ARCHER]*2, 2))
                    {
                        Attack(currUnit, currEnemy);
                    }
                }
                foreach (int soldier in enemySoldiers)
                {
                    Unit currEnemy = GameManager.Instance.GetUnit(soldier);
                    if ((currEnemy.GridPosition - currUnit.GridPosition).sqrMagnitude < Mathf.Pow(Constants.ATTACK_RANGE[UnitType.ARCHER]*2, 2))
                    {
                        Attack(currUnit, currEnemy);
                    }
                }
                foreach (int worker in enemyWorkers)
                {
                    Unit currEnemy = GameManager.Instance.GetUnit(worker);
                    if ((currEnemy.GridPosition - currUnit.GridPosition).sqrMagnitude < Mathf.Pow(Constants.ATTACK_RANGE[UnitType.ARCHER] * 2, 2))
                    {
                        Attack(currUnit, currEnemy);
                    }
                }
            }
        }
        public void WorkerGather()
        {
            // For each worker, gather
            foreach (int worker in myWorkers)
            {
                Unit unit = GameManager.Instance.GetUnit(worker);

                // Make sure this unit actually exists and is idle
                if (unit != null && unit.CurrentAction == UnitAction.IDLE && mainBaseNbr >= 0 && mainMineNbr >= 0)
                {
                    // Grab the mine
                    Unit mineUnit = GameManager.Instance.GetUnit(mainMineNbr);
                    Unit baseUnit = GameManager.Instance.GetUnit(mainBaseNbr);
                    if (mineUnit != null && baseUnit != null && mineUnit.Health > 0)
                    {
                        Gather(unit, mineUnit, baseUnit);
                    }
                }
            }
        }

        //get location on path closest to target
        public Vector3Int FindLocationNearPath(float target)
        {
            //rally troooooopps using path
            Unit baseUnit1 = GameManager.Instance.GetUnit(mainBaseNbr);
            Unit baseUnit2;
            if (enemyBases.Count != 0)
            {
                baseUnit2 = GameManager.Instance.GetUnit(enemyBases[0]);
            }
            else if(enemyBarracks.Count != 0)
            {
                baseUnit2 = GameManager.Instance.GetUnit(enemyBarracks[0]);
            }
            else if(enemyRefineries.Count != 0)
            {
                baseUnit2 = GameManager.Instance.GetUnit(enemyRefineries[0]);
            }
            else
            {
                return Vector3Int.zero;
            }
            //List<Vector3Int> nearby1 = GameManager.Instance.GetGridPositionsNearUnit(UnitType.BASE, soldier.GridPosition);
            List<Vector3Int> nearby1 = GameManager.Instance.GetGridPositionsNearUnit(UnitType.BASE, baseUnit1.GridPosition);
            List<Vector3Int> nearby2 = GameManager.Instance.GetGridPositionsNearUnit(UnitType.BASE, baseUnit2.GridPosition);
            Vector3Int newTarget = Vector3Int.zero;
            if (nearby1.Count != 0 && nearby2.Count != 0)
            {
                List<Vector3Int> path = GameManager.Instance.GetPathBetweenGridPositions(nearby1[Random.Range(0, nearby1.Count - 1)], nearby2[Random.Range(0, nearby2.Count - 1)]);

                if (path.Count != 0)
                {
                    newTarget = path[(int)(path.Count * target)];
                }
            }
            return newTarget;
        }
        public override void Update()
        {
            UpdateGameState();
            if (mines.Count == 1)
            {
                mainMineNbr = mines[0];
            }

            switch (actualPlayerState)
            {

                #region building
                case playerState.building:

                    // If we don't have 2 bases, build a base
                    if (myBases.Count == 0)
                    {
                        mainBaseNbr = -1;
                        //build this close to a mine
                        if (mainMineNbr != -1)
                        {
                            Unit mineUnit = GameManager.Instance.GetUnit(mainMineNbr);
                            BuildBuilding(UnitType.BASE, mineUnit);
                        }
                    }
                    // If we have at least one base, assume the first one is our "main" base
                    if (myBases.Count > 0)
                    {
                        mainBaseNbr = myBases[0];
                        //Debug.Log("BaseNbr " + mainBaseNbr);
                        //Debug.Log("MineNbr " + mainMineNbr);
                    }
                    if (mines.Count > 1)
                    {
                        //get the closer one
                        Unit mine1 = GameManager.Instance.GetUnit(mines[0]);
                        Unit mine2 = GameManager.Instance.GetUnit(mines[1]);
                        Unit worker = GameManager.Instance.GetUnit(myWorkers[0]);

                        float mine1Dist = (mine1.GridPosition - worker.GridPosition).sqrMagnitude;
                        float mine2Dist = (mine2.GridPosition - worker.GridPosition).sqrMagnitude;
                        if (mine2Dist < mine1Dist) { mainMineNbr = mines[1]; }
                        else { mainMineNbr = mines[0]; }
                    }
                    else
                    {
                        mainMineNbr = -1;
                    }
                    // If we don't have any barracks, build a barracks
                    if (myBarracks.Count == 0)
                    {
                        Unit mineUnit = GameManager.Instance.GetUnit(mainMineNbr);
                        BuildBuilding(UnitType.BARRACKS, mineUnit);
                    }
                    if (myBases.Count > 0 && myBarracks.Count > 0)
                    {
                        Debug.Log("<color=red> EXIT? </color>");
                        actualPlayerState = playerState.setup;
                    }
                    break;
                #endregion
                #region setup
                case playerState.setup:
                    
                    //go back
                    if(myBarracks.Count == 0 || myBases.Count < 1)// <2 if you want more workers
                    {
                        actualPlayerState = playerState.building;
                    }
                    //build more barracks
                    if(myBarracks.Count < 2)
                    {
                        Unit baseUnit = GameManager.Instance.GetUnit(mainBaseNbr);
                        BuildBuilding(UnitType.BARRACKS, baseUnit);
                    }

                    //train
                    WorkerTrainer();
                    ArmyTrainer();
                    WorkerGather();

                    //rally close to base

                    target = FindLocationNearPath(0.15f);
                    ArmyRally(mySoldiers, target);
                    ArmyRally(myArchers, target);

                    if(mySoldiers.Count > 20 && myArchers.Count > 10)
                    {
                        actualPlayerState = playerState.attack;
                    }
                    break;
                #endregion
                #region attack
                case playerState.attack:
                    //keep workin your soldiers
                    WorkerTrainer();
                    ArmyTrainer();
                    WorkerGather();

                    ArmyRally(mySoldiers, target);
                    ArmyRally(myArchers, target);
                    // For any troops, attack the enemy
                    AttackEnemy(mySoldiers);
                    AttackEnemy(myArchers);

                    //if i have an overwhelming force
                    if(mySoldiers.Count/0.75 >= enemySoldiers.Count && myArchers.Count / 0.75 >= enemyArchers.Count)
                    {
                        actualPlayerState = playerState.winning;
                    }
                    //if i dont have many, fall back
                    else if(mySoldiers.Count < enemySoldiers.Count/4 && myArchers.Count >= enemyArchers.Count/4)
                    {
                        actualPlayerState = playerState.setup;
                    }
                    break;
                #endregion

                #region winning
                case playerState.winning:
                    Debug.Log("<color=red> FUCK AAAAAAAAAAAA </color>");
                    //rally far away
                    //target = FindLocationNearPath(0.75f);
                    //ArmyRally(mySoldiers, target);
                    //ArmyRally(myArchers, target);

                    ArmyTrainer();
                    WorkerGather();
                    // For any troops, attack the enemy
                    AttackEnemy(mySoldiers);
                    AttackEnemy(myArchers);
                    break;
                #endregion
                default:
                    break;
            }

            #region reference code
            /*if (mines.Count > 0)
            {
                mainMineNbr = mines[0];
            }
            else
            {
                mainMineNbr = -1;
            }

            // If we have at least one base, assume the first one is our "main" base
            if (myBases.Count > 0)
            {
                mainBaseNbr = myBases[0];
                Debug.Log("BaseNbr " + mainBaseNbr);
                Debug.Log("MineNbr " + mainMineNbr);
            }

            // If we don't have 2 bases, build a base
            if (myBases.Count == 0)
            {
                mainBaseNbr = -1;

                BuildBuilding(UnitType.BASE);
            }

            // If we don't have any barracks, build a barracks
            if (myBarracks.Count == 0)
            {
                BuildBuilding(UnitType.BARRACKS);
            }

            // If we don't have any barracks, build a barracks
            if (myRefineries.Count == 0)
            {
                BuildBuilding(UnitType.REFINERY);
            }

            
            // For each base, determine if it should train a worker
            foreach (int baseNbr in myBases)
            {
                // Get the base unit
                Unit baseUnit = GameManager.Instance.GetUnit(baseNbr);

                // If the base exists, is idle, we need a worker, and we have gold
                if (baseUnit != null && baseUnit.IsBuilt
                                     && baseUnit.CurrentAction == UnitAction.IDLE
                                     && Gold >= Constants.COST[UnitType.WORKER]
                                     && myWorkers.Count < MAX_NBR_WORKERS)
                {
                    Train(baseUnit, UnitType.WORKER);
                }
            }

            // For each barracks, determine if it should train a soldier or an archer //
            foreach (int barracksNbr in myBarracks)
            {
                Unit barracksUnit = GameManager.Instance.GetUnit(barracksNbr);

                // If this barracks still exists, is idle, we need archers, and have gold
                if (barracksUnit != null && barracksUnit.IsBuilt
                         && barracksUnit.CurrentAction == UnitAction.IDLE
                         && Gold >= Constants.COST[UnitType.ARCHER])
                {
                    Train(barracksUnit, UnitType.ARCHER);
                }
                // If this barracks still exists, is idle, we need soldiers, and have gold
                if (barracksUnit != null && barracksUnit.IsBuilt
                    && barracksUnit.CurrentAction == UnitAction.IDLE
                    && Gold >= Constants.COST[UnitType.SOLDIER])
                {
                    Train(barracksUnit, UnitType.SOLDIER);
                }
            }

            // For each worker
            foreach (int worker in myWorkers)
            {
                // Grab the unit we need for this function
                Unit unit = GameManager.Instance.GetUnit(worker);

                // Make sure this unit actually exists and is idle
                if (unit != null && unit.CurrentAction == UnitAction.IDLE && mainBaseNbr >= 0 && mainMineNbr >= 0)
                {
                    // Grab the mine
                    Unit mineUnit = GameManager.Instance.GetUnit(mainMineNbr);
                    Unit baseUnit = GameManager.Instance.GetUnit(mainBaseNbr);
                    if (mineUnit != null && baseUnit != null && mineUnit.Health > 0)
                    {
                        Gather(unit, mineUnit, baseUnit);
                    }
                }
            }*/
            #endregion
        }

        #endregion
    }
}

