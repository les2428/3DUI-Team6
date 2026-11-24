using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThreeDUI.Tests
{
    /// <summary>Tutorial 4 · the guide agent in the T4 example: tool calls, the guardrail, walking over, proxemics.</summary>
    public class GuideAgentTests
    {
        private IEnumerator Load()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Template/Examples/T4_SpatialUI.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            for (int i = 0; i < 10; i++) yield return null;
        }

        [Test]
        public void ParseReply_KeepsOnlyKnownWords()
        {
            var act = GuideAgent.ParseReply("Sure! ```json {\"say\":\"Hi\",\"gesture\":\"dance\",\"action\":\"fly_to\",\"target\":\"golden_cat\"} ```");
            Assert.AreEqual("Hi", act.say);
            Assert.AreEqual("none", act.gesture, "unknown gesture");
            Assert.AreEqual("none", act.action, "unknown action");
            Assert.AreEqual("", act.target, "invented target");
            Assert.AreEqual("shrug", GuideAgent.ParseReply("no json at all").gesture);
        }

        [Test]
        public void LlmRequest_HasTheContractAndTheScene()
        {
            string sys = GuideLlm.SystemPrompt("Nova");
            StringAssert.Contains("walk_to", sys);
            StringAssert.Contains("point_at", sys);
            string user = GuideLlm.UserMessage("- globe: Globe | A globe.", "show me the \"blue\" one");
            string json = GuideLlm.BuildRequestJson("m", sys, user, true);
            StringAssert.Contains("\\\"blue\\\"", json, "quotes escaped");
            StringAssert.Contains("\"response_format\":{\"type\":\"json_object\"}", json);
            StringAssert.Contains("The visitor says:", user);
        }

        [Test]
        public void Rules_ArrivedAtAThing_PointsAtIt()
        {
            string scene = "The visitor is 2.0 m from you (social zone).\nThings you can talk about (id: name | what to say):\n- globe: Globe | A globe.\n";
            string reply = GuideBrain.Reply(scene, "[event] arrived at globe");   // walked there after an LLM turn: nothing was clicked
            StringAssert.Contains("\"action\":\"point_at\"", reply);
            StringAssert.Contains("\"target\":\"globe\"", reply);
        }

        [Test]
        public void Zones_FollowHall()
        {
            Assert.AreEqual("intimate", GuideAgent.Zone(0.3f));
            Assert.AreEqual("personal", GuideAgent.Zone(1.0f));
            Assert.AreEqual("social", GuideAgent.Zone(2.0f));
            Assert.AreEqual("public", GuideAgent.Zone(5.0f));
        }

        [UnityTest]
        public IEnumerator ClickThing_AgentWalksOverPointsAndSpeaks()
        {
            yield return Load();
            var agent = Object.FindAnyObjectByType<GuideAgent>();
            var globe = GuideThing.Find("globe");
            Assert.IsNotNull(agent); Assert.IsNotNull(globe);
            Assert.AreEqual(3, GuideThing.All.Count, "statue, globe, painting");
            agent.thinkSeconds = 0.1f;
            while (agent.Busy) yield return null;

            globe.ClickFromCode();
            yield return new WaitForSeconds(0.3f);
            StringAssert.Contains("\"action\":\"walk_to\"", agent.LastReply);
            StringAssert.Contains("\"target\":\"globe\"", agent.LastReply);
            for (float t = 0; t < 12f && agent.LastAct.action != "point_at"; t += Time.deltaTime) yield return null;
            Assert.AreEqual("point_at", agent.LastAct.action, "after arriving it points");
            Assert.Less(GuideAgent.Flat(agent.transform.position, globe.Center), 1.3f, "it walked over to the globe");
            StringAssert.Contains("globe", agent.LastAct.say.ToLowerInvariant());
            Assert.AreEqual("point", agent.GetComponent<GuideBody>().Gesture);
        }

        [UnityTest]
        public IEnumerator Visitor_TooClose_AgentStepsBack()
        {
            yield return Load();
            var agent = Object.FindAnyObjectByType<GuideAgent>();
            var rig = PlayerRig.Instance;
            agent.thinkSeconds = 0.1f;
            Vector3 toward = agent.transform.position + agent.transform.forward * 0.6f;   // 0.6 m: personal zone
            rig.transform.position += toward - rig.FeetPosition;
            yield return new WaitForSeconds(1.5f);
            Assert.GreaterOrEqual(GuideAgent.Flat(agent.transform.position, rig.FeetPosition), agent.keepDistance - 0.05f,
                "a stranger keeps the social zone");
        }
    }
}
