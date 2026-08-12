using NUnit.Framework;
using VLTK.Production.App;
using VLTK.Production.Networking;

namespace VLTK.Tests.Production.EditMode
{
    public sealed class ProductionBootStateMachineTests
    {
        [Test]
        public void BootFlow_ReachesReady_WithCanonicalMap53AndWss()
        {
            var machine = new ProductionBootStateMachine();
            Assert.That(machine.BeginBootstrap(), Is.True);
            Assert.That(machine.ApplyBootstrap(ValidBootstrap()), Is.True);
            Assert.That(machine.ApplyAuth(ValidAuth()), Is.True);
            Assert.That(machine.ApplyCharacter(ValidCharacter()), Is.True);
            Assert.That(machine.ApplyContent(ValidContent()), Is.True);
            Assert.That(machine.ApplyRealtimeAdmission(new RealtimeAdmissionResult(true, null)), Is.True);
            Assert.That(machine.ApplyMapLoaded(53), Is.True);
            Assert.That(machine.ApplyAvatarPresented(), Is.True);
            Assert.That(machine.ApplyJoystickReady(), Is.True);
            Assert.That(machine.State, Is.EqualTo(ProductionBootState.Ready));
        }

        [Test]
        public void Bootstrap_FailsClosed_WhenApiBaseUrlIsNotHttps()
        {
            var machine = new ProductionBootStateMachine();
            machine.BeginBootstrap();
            var response = ValidBootstrap();
            response.apiBaseUrl = "http://realm.example/api";
            Assert.That(machine.ApplyBootstrap(response), Is.False);
            Assert.That(machine.State, Is.EqualTo(ProductionBootState.Failed));
            Assert.That(machine.FailureCode, Is.EqualTo("bootstrap_invalid"));
        }

        [Test]
        public void Character_FailsClosed_WhenMapIsNotCanonical53()
        {
            var machine = new ProductionBootStateMachine();
            machine.BeginBootstrap();
            machine.ApplyBootstrap(ValidBootstrap());
            machine.ApplyAuth(ValidAuth());
            var character = ValidCharacter();
            character.selectedCharacter.mapId = 79;
            Assert.That(machine.ApplyCharacter(character), Is.False);
            Assert.That(machine.FailureCode, Is.EqualTo("character_invalid"));
        }

        [Test]
        public void SecretRedactor_RedactsPasswordTokensAndAdmissionTickets()
        {
            string redacted = SecretRedactor.RedactMessage("password=hunter2 accessToken=abc refresh_token=def admissionTicket=ghi ok=1");
            Assert.That(redacted, Does.Not.Contain("hunter2"));
            Assert.That(redacted, Does.Not.Contain("abc"));
            Assert.That(redacted, Does.Not.Contain("def"));
            Assert.That(redacted, Does.Not.Contain("ghi"));
            Assert.That(redacted, Does.Contain("ok=1"));
        }

        private static RealmBootstrapResponse ValidBootstrap()
        {
            return new RealmBootstrapResponse
            {
                realmId = "p1",
                apiBaseUrl = "https://realm.example/api",
                clientVersion = "editor-p1"
            };
        }

        private static AuthSessionResponse ValidAuth()
        {
            return new AuthSessionResponse
            {
                accountId = "10000000-0000-0000-0000-000000000001",
                realmId = "p1",
                accessToken = "access-token",
                accessExpiresAt = "2030-01-01T00:00:00Z",
                refreshToken = "refresh-token",
                refreshExpiresAt = "2030-02-01T00:00:00Z"
            };
        }

        private static CharacterSelectionResponse ValidCharacter()
        {
            return new CharacterSelectionResponse
            {
                admissionTicket = "ticket",
                realtimeEndpoint = "wss://realm.example/game",
                selectedCharacter = new CharacterSummary { characterId = "20000000-0000-0000-0000-000000000001", name = "Hero", gender = "male", homelandId = 1, level = 1, mapId = 53, slot = 1, appearanceRevision = 1, version = 1, spawnX = 1, spawnY = 2 }
            };
        }

        private static VerifiedContentResponse ValidContent()
        {
            return new VerifiedContentResponse
            {
                verified = true,
                mapId = 53,
                contentDigest = new ContentDigestDto
                {
                    contentReleaseId = "30000000-0000-0000-0000-000000000053",
                    manifestSha256 = new string('a', 64),
                    sourceSnapshotId = "map-runtime",
                    catalogUnionSize = 242,
                    catalogUnionSha256 = new string('b', 64),
                    runtimeSkillPolicyId = "p1",
                    clientProjectionSha256 = new string('c', 64)
                },
                trust = new ContentTrustResult(ContentTrustMode.EditorPinnedDigest, true, false, null),
                provenanceSha256 = new string('d', 64)
            };
        }
    }
}
