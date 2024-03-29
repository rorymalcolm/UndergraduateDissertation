// <copyright file="TestClientTest.cs">Copyright ©  2015</copyright>
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using hist_mmorpg;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.IO;
using ProtoBuf;

namespace hist_mmorpg.Tests1
{
    /// <summary>This class contains parameterized unit tests for TestClient</summary>
    [TestClass]
    public partial class TestClientTest
    {

        /// <summary>
        /// The Game object for this test (contains and handles all game data)
        /// </summary>
        public static Game game;
        /// <summary>
        /// The Server object used for this test (contains connected client information
        /// </summary>
        public static Server server;
        /// <summary>
        /// The dummy Client to be used for this test
        /// </summary>
        public static TestClient client;

        public static Army OwnedArmy;
        public static Army NotOwnedArmy;
        public static Fief OwnedFief;
        public static Fief NotOwnedFief;
        public static string Username;
        public static string Pass;
        public static string OtherUser;
        public static string OtherPass;
        public static string BadUsername;
        public static string BadPass;
        public static PlayerCharacter MyPlayerCharacter;
        public static PlayerCharacter NotMyPlayerCharacter;
        public static NonPlayerCharacter MyFamily;
        public static NonPlayerCharacter MyEmployee;
        public static NonPlayerCharacter NotMyFamily;
        public static NonPlayerCharacter NotMyEmplployee;
        public static NonPlayerCharacter NobodysCharacter;
        /// <summary>
        /// Initialise game state for the TestSuite
        /// </summary>
        /// <param name="ctx"></param>
        [ClassInitialize()]
        public static void InitialiseGameState(TestContext ctx = null)
        {
            Globals_Server.LogFile = new System.IO.StreamWriter("LogFile.txt");
            Globals_Server.LogFile.AutoFlush = true;
            game = new Game();
            server = new Server();
            client = new TestClient();
            Username = "helen";
            Pass = "potato";
            OtherUser = "test";
            OtherPass = "tomato";
            BadUsername = "notauser";
            BadPass = "notapass";
            MyPlayerCharacter = Globals_Game.ownedPlayerCharacters[Username];
            Dictionary<string, PlayerCharacter>.Enumerator e = Globals_Game.pcMasterList.GetEnumerator();
            e.MoveNext();
            NotMyPlayerCharacter = e.Current.Value;
            while (NotMyPlayerCharacter == MyPlayerCharacter)
            {
                e.MoveNext();
                NotMyPlayerCharacter = e.Current.Value;
            }
            foreach (NonPlayerCharacter npc in MyPlayerCharacter.myNPCs)
            {
                if (!string.IsNullOrWhiteSpace(npc.familyID))
                {
                    MyFamily = npc;
                }
                else if (!string.IsNullOrWhiteSpace(npc.employer))
                {
                    MyEmployee = npc;
                }
                if (MyEmployee != null && MyFamily != null)
                {
                    break;
                }
            }
            foreach (NonPlayerCharacter npc in NotMyPlayerCharacter.myNPCs)
            {
                if (!string.IsNullOrWhiteSpace(npc.familyID))
                {
                    NotMyFamily = npc;
                }
                else if (!string.IsNullOrWhiteSpace(npc.employer))
                {
                    NotMyEmplployee = npc;
                }
                if (NotMyEmplployee != null && NotMyFamily != null)
                {
                    break;
                }
            }
            if (MyPlayerCharacter.myArmies != null && MyPlayerCharacter.myArmies.Count > 0)
            {
                OwnedArmy = MyPlayerCharacter.myArmies[0];
            }
            else
            {
                Army army = new Army(Globals_Game.GetNextArmyID(), null, MyPlayerCharacter.charID, 30, NotMyPlayerCharacter.location.id, false, trp: new uint[] { 5, 5, 5, 5, 5, 5,5  });
                OwnedArmy = army;
                OwnedArmy.AddArmy();
            }
            if (NotMyPlayerCharacter.myArmies != null && NotMyPlayerCharacter.myArmies.Count > 0)
            {
                NotOwnedArmy = NotMyPlayerCharacter.myArmies[0];
            }
            else
            {
                Army army = new Army(Globals_Game.GetNextArmyID(), null, NotMyPlayerCharacter.charID, 30, NotMyPlayerCharacter.location.id, false, trp: new uint[] { 5, 5, 5, 5, 5, 5, 5 });
                NotOwnedArmy = army;
                NotOwnedArmy.AddArmy();

            }
            if (MyPlayerCharacter.ownedFiefs != null && MyPlayerCharacter.ownedFiefs.Count > 0)
            {
                OwnedFief = MyPlayerCharacter.ownedFiefs[0];
            }
            if (NotMyPlayerCharacter.ownedFiefs != null && NotMyPlayerCharacter.ownedFiefs.Count > 0)
            {
                NotOwnedFief = NotMyPlayerCharacter.ownedFiefs[0];
            }
            foreach (var npc in Globals_Game.npcMasterList)
            {
                if (npc.Value.GetPlayerCharacter() == null)
                {
                    NobodysCharacter = npc.Value;
                }
            }
            client.LogInAndConnect(Username,Pass,new byte[]{1,2,3,4,5,6});
            while (!client.IsConnectedAndLoggedIn())
            {
                Thread.Sleep(0);
            }
            client.ClearMessageQueues();
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            client.ClearMessageQueues();
        }

        [ClassCleanup()]
        public static void FinaliseGameState()
        {
            client.LogOut();
            server.Shutdown();
            // Sleep for a second to let writing to log file complete
            Thread.Sleep(1000);
            Globals_Server.LogFile.Close();
        }

        /// <summary>
        /// Assert that the test client is logged in, has a PlayerCharacter and is a registered observer
        /// </summary>
        /// <param name="testClient"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool ValidClientState(TestClient testClient, out Client client)
        {
            if (testClient.net == null || !testClient.IsConnectedAndLoggedIn())
            {
                client = null;
                return false;
            }
            // If do not have a PlayerCharacter, are dead etc, expect to fail
            Globals_Server.Clients.TryGetValue(testClient.playerID, out client);
            if (client == null)
            {
                Assert.AreEqual(testClient.net.GetConnectionStatusString(), "Disconnected");
                return false;
            }

            if (client.myPlayerCharacter == null || !client.myPlayerCharacter.isAlive)
            {
                Task<string> ReplyTask = testClient.GetServerMessage();
                ReplyTask.Wait();
                string reply = ReplyTask.Result;
                Assert.AreEqual(reply, "You have no valid PlayerCharacter!");
                return false;
            }
            if (!Globals_Game.IsObserver(client))
            {
                return false;
            }
            return true;
        }

        /// <summary>Test stub for AdjustExpenditure(String, Double, Double, Double, Double, Double)</summary>

        public void AdjustExpenditureTest(
            TestClient TestClient,
            string fiefID,
            double newTax,
            double newOff,
            double newGarr,
            double newKeep,
            double newInfra
        )
        {
            TestClient.AdjustExpenditure(fiefID, newTax, newOff, newGarr, newKeep, newInfra);

            Client client = null;
            if (!ValidClientState(TestClient, out client))
            {
                Task<string> ReplyTask = TestClient.GetServerMessage();
                ReplyTask.Wait();
                string reply = ReplyTask.Result;
                Assert.AreEqual("Not logged in- Disconnecting", reply);
                return;
            }
            // If not a valid fief, expect to fail
            if (string.IsNullOrWhiteSpace(fiefID) || !Globals_Game.fiefKeys.Contains(fiefID))
            {
                Console.Write("not a fief Id ");
                Task<ProtoMessage> ReplyTask = TestClient.GetReply();
                ReplyTask.Wait();
                ProtoMessage reply = ReplyTask.Result;
                Assert.AreEqual(reply.ResponseType, DisplayMessages.ErrorGenericFiefUnidentified);
            }
            else
            {
                
                Task<ProtoMessage> ReplyTask = TestClient.GetReply();
                ReplyTask.Wait();
                ProtoMessage reply = ReplyTask.Result;
                // If not fief owner expect unauthorised
                Fief fief = null;
                Globals_Game.fiefMasterList.TryGetValue(fiefID, out fief);
                if (fief.owner != client.myPlayerCharacter)
                {

                    Assert.AreEqual(reply.ResponseType, DisplayMessages.ErrorGenericUnauthorised);
                }
                // If numbers invalid expect an invalid exception
                else if (newTax < 0 || newOff < 0 || newGarr < 0 || newKeep < 0 || newInfra < 0)
                {
                    Assert.AreEqual(reply.ResponseType, DisplayMessages.ErrorGenericMessageInvalid);
                }
                else
                {
                    if (reply.GetType() == typeof(ProtoFief))
                    {
                        Assert.AreEqual(DisplayMessages.FiefExpenditureAdjusted, reply.ResponseType);
                    }
                    else
                    {
                        Assert.AreEqual(DisplayMessages.FiefExpenditureAdjustment, reply.ResponseType);
                    }
                }
            }

        }

        public void AttackTest(TestClient testClient, string armyID, string targetID)
        {
            testClient.Attack(armyID, targetID);
            Client client = null;
            if (!ValidClientState(testClient, out client))
            {
                Task<string> ReplyTask = testClient.GetServerMessage();
                ReplyTask.Wait();
                string reply = ReplyTask.Result;
                Assert.AreEqual("Invalid message sequence-expecting login", reply);
                return;
            }
            else
            {
                Task<ProtoMessage> ReplyTask = testClient.GetReply();
                ReplyTask.Wait();
                ProtoMessage reply = ReplyTask.Result;
                Army army = null;
                if (armyID != null)
                {
                    Globals_Game.armyMasterList.TryGetValue(armyID, out army);
                }

                Army targetArmy = null;
                if (targetID != null)
                {
                    Globals_Game.armyMasterList.TryGetValue(targetID, out targetArmy);
                }

                ProtoMessage tmp = null;
                if (army == null || targetArmy == null)
                {
                    Assert.IsTrue(reply.ResponseType == DisplayMessages.ErrorGenericMessageInvalid || reply.ResponseType == DisplayMessages.ErrorGenericArmyUnidentified || reply.ResponseType == DisplayMessages.Error);
                }
                else if (army.CalcArmySize() == 0 || targetArmy.CalcArmySize() == 0)
                {
                    Assert.AreEqual(DisplayMessages.Error, reply.ResponseType);
                }
                else if (!client.myPlayerCharacter.myArmies.Contains(army))
                {
                    Assert.AreEqual(DisplayMessages.ErrorGenericUnauthorised, reply.ResponseType);
                }
                else if (client.myPlayerCharacter.myArmies.Contains(targetArmy))
                {
                    Assert.AreEqual(DisplayMessages.ArmyAttackSelf, reply.ResponseType);
                }
                else if (!army.ChecksBeforeAttack(targetArmy, out tmp))
                {
                    Assert.AreEqual(tmp.ResponseType, reply.ResponseType);
                }
                else
                {
                    Assert.IsTrue(reply.ResponseType == DisplayMessages.BattleBringSuccess || reply.ResponseType == DisplayMessages.BattleBringFail || reply.ResponseType == DisplayMessages.BattleResults);
                }
            }
        }

        /// <summary>Test stub for LogIn(String, String, Byte[])</summary>

        public void LogInTest(
            TestClient client,
            string user,
            string pass,
            byte[] key
        )
        {
            client.LogInAndConnect(user, pass, key);
            // If username not recognised, expect to be disconnected
            if (string.IsNullOrEmpty(user) || !Utility_Methods.CheckStringValid("combined", user) || !LogInManager.users.ContainsKey(user))
            {
                Assert.AreEqual("Disconnected", client.net.GetConnectionStatusString());
                return;
            }
            // If password is incorrect, expect an error
            Tuple<byte[], byte[]> hashNsalt = LogInManager.users[user];
            byte[] hash;
            if (pass == null)
            {
                hash = null;
            }
            else
            {
                hash = LogInManager.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pass), hashNsalt.Item2);
            }
            if (hash == null || !hashNsalt.Item1.SequenceEqual(hash) || key == null || key.Length < 5)
            {
                Assert.AreEqual("Disconnected", client.net.GetConnectionStatusString());
                Assert.IsFalse(Server.ContainsConnection(user));
            }
            else
            {
                // If the login was successful, expecting a ProtoLogin followed by a ProtoClient back
                Task<ProtoMessage> getReply = client.GetReply();
                getReply.Wait();
                ProtoMessage reply = getReply.Result;
                Assert.AreEqual(reply.GetType(), typeof(ProtoLogIn));
                while (!client.IsConnectedAndLoggedIn())
                {
                    Thread.Sleep(0);
                }
                // If login was successful, the client should be in the list of registered observers
                Assert.IsTrue(Globals_Game.IsObserver(Globals_Server.Clients[user]));
                Assert.IsTrue(Server.ContainsConnection(user));
            }
        }


        /// <summary>Test stub for MaintainArmy(String)</summary>

        public DisplayMessages MaintainArmyTest(TestClient target, string armyID)
        {
            Client client;
            if (!ValidClientState(target, out client))
            {
                Assert.IsTrue(target.IsConnectedAndLoggedIn()==false);
            }
            int treasury = client.myPlayerCharacter.GetHomeFief().GetAvailableTreasury(true);
            uint armyMaintenanceCost=0;
            bool isMaintained = false;
            DisplayMessages armyError;
            Army army = Utility_Methods.GetArmy(armyID, out armyError);
            if (army != null)
            {
                armyMaintenanceCost = army.getMaintenanceCost();
                isMaintained = army.isMaintained;
            }
            
            target.MaintainArmy(armyID);
            var replyTask = target.GetReply();
            if (!replyTask.Wait(5000))
            {
                Assert.Fail("Timed out");

            }
            var result = replyTask.Result;
            
            // if armyID is null or empty, or a bad format, get a MessageInvalid message
            if (string.IsNullOrWhiteSpace(armyID) || !Utility_Methods.ValidateArmyID(armyID))
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericMessageInvalid,result.ResponseType);
                return result.ResponseType;
            }
            // If armyID is not a recognised army, get an ArmyUnidentified message
            if (!Globals_Game.armyMasterList.ContainsKey(armyID)||army==null)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericArmyUnidentified,result.ResponseType);
                return result.ResponseType;
            }
            // If the player does not own the army, get an Unauthorised message
            if (army.GetOwner() != client.myPlayerCharacter)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericUnauthorised,result.ResponseType);
                return result.ResponseType;
            }
            // If the army has already been maintained, expect a MaintainAlready message
            if (isMaintained)
            {
                Assert.AreEqual(DisplayMessages.ArmyMaintainedAlready,result.ResponseType);
                return result.ResponseType;
            }
            // If don't have enough funds, 
            if (treasury<armyMaintenanceCost)
            {
                Assert.AreEqual(DisplayMessages.ArmyMaintainInsufficientFunds,result.ResponseType);
                return result.ResponseType;
            }
            // If all the checks pass, expect an army back
            ProtoArmy armyDetails = result as ProtoArmy;
            if (armyDetails == null)
            {
                Assert.Fail("Could not parse result to ProtoArmy");
            }
            return result.ResponseType;
        }

        /// <summary>Test stub for Marry(String, String)</summary>

        public void MarryTest(
            TestClient target,
            string groomID,
            string brideID
        )
        {
            target.Marry(groomID, brideID);
            // TODO: add assertions to method TestClientTest.MarryTest(TestClient, String, String)
        }

        /// <summary>Test stub for Move(String, String, String[])</summary>

        public void MoveTest(
            TestClient target,
            string character,
            string location,
            string[] travelInstructions
        )
        {
            target.Move(character, location, travelInstructions);
            // TODO: add assertions to method TestClientTest.MoveTest(TestClient, String, String, String[])
        }

        /// <summary>Test stub for RansomCaptive(String)</summary>

        public void RansomCaptiveTest(TestClient target, string charID)
        {
            target.RansomCaptive(charID);
            // TODO: add assertions to method TestClientTest.RansomCaptiveTest(TestClient, String)
        }


        /// <summary>
        /// Test stub for spy character
        /// </summary>
        /// <param name="testClient"></param>
        /// <param name="charID"></param>
        /// <param name="targetID"></param>
        /// <param name="DoSpy"></param>

        public void SpyCharacterTest(TestClient testClient, string charID, string targetID, bool DoSpy)
        {
            bool ownsCharacter = true;
            Character spy = Globals_Game.getCharFromID(charID);
            Client client = Globals_Server.Clients[testClient.playerID];
            if (spy != null)
            {
                ownsCharacter = (spy.GetPlayerCharacter().Equals(client.myPlayerCharacter));
            }
            testClient.SpyOnCharacter(charID, targetID);
            Task<ProtoMessage> responseTask = testClient.GetReply();
            responseTask.Wait();
            ProtoMessage response = responseTask.Result;
            // If don't identify either character, CharacterUnidentified error
            // If don't own spy, PermissionDenied error
            // If target is own character, SpyCharacterOwn error
            // If target is not in same fief as spy, TooFarFromFief error
            // If target is not valid, other error
            // Otherwise, expect a response

            if (string.IsNullOrWhiteSpace(charID) || string.IsNullOrWhiteSpace(targetID))
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericMessageInvalid, response.ResponseType);
                return;
            }
            Character target = Globals_Game.getCharFromID(targetID);
            if (spy == null || target == null)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericCharacterUnidentified, response.ResponseType);
                return;
            }
            
            if (!ownsCharacter)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericUnauthorised, response.ResponseType);
                return;
            }
            if (spy.location != target.location)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericNotInSameFief, response.ResponseType);
                return;
            }
            if(spy.days<10)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericNotEnoughDays, response.ResponseType);
                return;
            }
            if(target.GetPlayerCharacter()==spy.GetPlayerCharacter())
            {
                Assert.AreEqual(DisplayMessages.ErrorSpyOwn, response.ResponseType);
                return;
            }
        }

        /// <summary>
        /// Test stub for spy character
        /// </summary>
        /// <param name="testClient"></param>
        /// <param name="charID"></param>
        /// <param name="targetID"></param>
        /// <param name="DoSpy"></param>

        public void SpyArmyTest(TestClient testClient, string charID, string targetID)
        {
            testClient.SpyOnArmy(charID, targetID);
            Task<ProtoMessage> responseTask = testClient.GetReply();
            responseTask.Wait();
            ProtoMessage response = responseTask.Result;
            if (string.IsNullOrWhiteSpace(charID) || string.IsNullOrWhiteSpace(targetID))
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericMessageInvalid, response.ResponseType);
                return;
            }
            Character spy = Globals_Game.getCharFromID(charID);
            Army target = null;
            Globals_Game.armyMasterList.TryGetValue(targetID, out target);
            if (spy == null || target == null)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericCharacterUnidentified, response.ResponseType);
                return;
            }
            Client client = Globals_Server.Clients[testClient.playerID];
            if (spy.GetPlayerCharacter() != client.myPlayerCharacter)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericUnauthorised, response.ResponseType);
                return;
            }
            if (spy.location != target.GetLocation())
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericNotInSameFief, response.ResponseType);
                return;
            }
            if(!(spy.days>=10))
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericNotEnoughDays, response.ResponseType);
                return;
            }
        }
        /// <summary>
        /// Test stub for spy character
        /// </summary>
        /// <param name="testClient"></param>
        /// <param name="charID"></param>
        /// <param name="targetID"></param>
        /// <param name="DoSpy"></param>

        public void SpyFiefTest(TestClient testClient, string charID, string targetID, bool DoSpy)
        {
            Character spy = Globals_Game.getCharFromID(charID);
            Client client = Globals_Server.Clients[testClient.playerID];
            bool ownSpy = true;
            if (spy != null)
            {
                ownSpy= (spy.GetPlayerCharacter().Equals(client.myPlayerCharacter));
            }
            testClient.SpyOnFief(charID, targetID);
            Task<ProtoMessage> responseTask = testClient.GetReply();
            responseTask.Wait();
            ProtoMessage response = responseTask.Result;

            if (string.IsNullOrWhiteSpace(charID) || string.IsNullOrWhiteSpace(targetID))
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericMessageInvalid, response.ResponseType);
                return;
            }
            Fief target = null;
            Globals_Game.fiefMasterList.TryGetValue(targetID, out target);
            if (spy == null || target == null)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericCharacterUnidentified, response.ResponseType);
                return;
            }
            if (!ownSpy)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericUnauthorised, response.ResponseType);
                return;
            }
            if (spy.location != target)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericNotInSameFief, response.ResponseType);
                return;
            }
            if(spy.days<10)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericNotEnoughDays, response.ResponseType);
                return;
            }
        }

        /// <summary>Test stub for RecruitTroops(String, UInt32, Boolean)</summary>

        public void RecruitTroopsTest(
            TestClient target,
            string armyID,
            uint numTroops,
            bool isConfirm
        )
        {
            bool ValidRecruit = true;
            Client client = Globals_Server.Clients[target.playerID];
            ProtoMessage ignoreMessage;
            ValidRecruit = client.myPlayerCharacter.ChecksBeforeRecruitment(out ignoreMessage);
            target.RecruitTroops(armyID, numTroops, isConfirm);
            Thread.Sleep(5000);
            Army army = null;
            Task<ProtoMessage> responseTask = target.GetReply();
            responseTask.Wait();
            ProtoMessage response = responseTask.Result;
            if (!string.IsNullOrWhiteSpace(armyID))
            {
                if (!Globals_Game.armyMasterList.ContainsKey(armyID))
                {
                    // Expect army unrecognised
                    Assert.AreEqual(DisplayMessages.ErrorGenericArmyUnidentified, response.ResponseType);
                    return;
                }
                else
                {
                    army = Globals_Game.armyMasterList[armyID];
                }
            }
            else
            {
                return;
            }
            if (army != null)
            {
                if (army.GetOwner() != client.myPlayerCharacter)
                {
                    Assert.AreEqual(DisplayMessages.ErrorGenericUnauthorised, response.ResponseType);
                }
                if (army.GetLocation().CalcMaxTroops() < numTroops)
                {
                    Assert.AreEqual(DisplayMessages.CharacterRecruitInsufficientFunds, response.ResponseType);
                }
            }
            if (!ValidRecruit)
            {
                Assert.IsFalse(response.ResponseType == DisplayMessages.CharacterRecruitInsufficientFunds || response.ResponseType == DisplayMessages.CharacterRecruitInsufficientFunds);
                Assert.IsFalse(response is ProtoRecruit);
                return;
            }
            ProtoRecruit recruitDetails = response as ProtoRecruit;
            if (recruitDetails == null)
            {
                Assert.Fail();
            }
        }

        public void TravelTest(TestClient target, string charID, string targetFief, string[] travelInstructions)
        {
            DisplayMessages fiefError,charError;
            Character character = Utility_Methods.GetCharacter(charID, out charError);
            Client client = Globals_Server.Clients[target.playerID];
            Fief oldFief = null;
            if (character != null)
            {
                oldFief = character.location;
            }
            target.Move(charID,targetFief,travelInstructions);
            Task<ProtoMessage> responseTask = target.GetReply();
            responseTask.Wait();
            ProtoMessage reply = responseTask.Result;

            // Travelling will be done either by series of instructions or by choosing fief- ensure at least one is being used!
            if (string.IsNullOrWhiteSpace(targetFief))
            {
                if (travelInstructions == null)
                {
                    Assert.AreEqual(DisplayMessages.ErrorGenericFiefUnidentified,reply.ResponseType);
                    return;
                }
            }
            Fief f = Utility_Methods.GetFief(targetFief, out fiefError);
            if (!string.IsNullOrWhiteSpace(targetFief) && f == null)
            {
                Assert.AreEqual(fiefError,reply.ResponseType);
                return;
            }

            // Note- if the travel instructions are invalid the server will still move the client up until the first invalid instruction, and will then send an update stating that there was a problem with the movement instructions

            
            if (character == null)
            {
                Assert.AreEqual(charError,reply.ResponseType);
                return;
            }
            if (character.GetPlayerCharacter() != client.myPlayerCharacter)
            {
                Assert.AreEqual(DisplayMessages.ErrorGenericUnauthorised, reply.ResponseType);
                return;
            }
            if (f != null && character.days < oldFief.getTravelCost(f))
            {
                Assert.AreEqual(DisplayMessages.CharacterDaysJourney,reply.ResponseType);
                return;
            }
            ProtoFief fiefDetails = reply as ProtoFief;
            Assert.IsNotNull(fiefDetails);
            
        }

    }
}
