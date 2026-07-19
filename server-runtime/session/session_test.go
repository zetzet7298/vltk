package session

import "testing"

func TestSequencingIdempotencyAndOutOfOrder(t *testing.T) {
	s := New(7, 4)
	if got := s.Accept(7, 1, "a").Disposition; got != CommandAccepted {
		t.Fatalf("accept got %v", got)
	}
	if got := s.Accept(7, 1, "a").Disposition; got != CommandDuplicate {
		t.Fatalf("dup got %v", got)
	}
	if got := s.Accept(7, 3, "c").Disposition; got != CommandOutOfOrder {
		t.Fatalf("gap got %v", got)
	}
}

func TestReconnectGraceReplaySnapshotFallbackAndAvatarActive(t *testing.T) {
	s := New(1, 2)
	s.AppendServerEvent(1, []byte("one"))
	s.AppendServerEvent(2, []byte("two"))
	s.AppendServerEvent(3, []byte("three"))
	s.Disconnect(10)
	if !s.AvatarTargetable(10 + ReconnectGraceTicks) {
		t.Fatalf("avatar must remain targetable during grace")
	}
	outcome, replay, err := s.Reconnect(1, 2, 20)
	if err != nil || outcome != ResumeDeltaReplay || len(replay) != 1 || string(replay[0].Data) != "three" {
		t.Fatalf("delta outcome=%v replay=%v err=%v", outcome, replay, err)
	}
	s.Disconnect(30)
	outcome, _, err = s.Reconnect(1, 0, 31)
	if err != nil || outcome != ResumeFullSnapshot {
		t.Fatalf("snapshot outcome=%v err=%v", outcome, err)
	}
	s.Disconnect(40)
	outcome, _, err = s.Reconnect(1, 3, 40+ReconnectGraceTicks+1)
	if err == nil || outcome != ResumeGraceExpired {
		t.Fatalf("grace outcome=%v err=%v", outcome, err)
	}
}
