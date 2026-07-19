package session

import "errors"

const (
	TickRateHz          = 18
	ReconnectGraceTicks = 15 * TickRateHz
)

var (
	ErrEpochMismatch = errors.New("session: epoch mismatch")
	ErrOutOfOrder    = errors.New("session: client seq out of order")
	ErrGraceExpired  = errors.New("session: reconnect grace expired")
)

type ResumeOutcome uint8

const (
	ResumeNewSession ResumeOutcome = iota + 1
	ResumeDeltaReplay
	ResumeFullSnapshot
	ResumeGraceExpired
	ResumeBaselineMismatch
	ResumeSessionReplaced
)

type CommandDisposition uint8

const (
	CommandAccepted CommandDisposition = iota + 1
	CommandDuplicate
	CommandOutOfOrder
	CommandEpochMismatch
)

type CommandReceipt struct {
	Disposition CommandDisposition
	LastSeq     uint64
}

func (d CommandDisposition) String() string {
	switch d {
	case CommandAccepted:
		return "ACCEPTED"
	case CommandDuplicate:
		return "DUPLICATE"
	case CommandOutOfOrder:
		return "OUT_OF_ORDER"
	case CommandEpochMismatch:
		return "EPOCH_MISMATCH"
	default:
		return "UNKNOWN"
	}
}

type ServerEvent struct {
	Seq  uint64
	Tick uint64
	Data []byte
}

type Session struct {
	Epoch              uint64
	ExpectedClientSeq  uint64
	LastProcessedSeq   uint64
	LastServerSeq      uint64
	DisconnectedAtTick uint64
	Connected          bool
	commands           map[uint64]string
	replay             []ServerEvent
	maxReplay          int
}

func New(epoch uint64, maxReplay int) *Session {
	if maxReplay <= 0 {
		maxReplay = 128
	}
	return &Session{Epoch: epoch, ExpectedClientSeq: 1, Connected: true, commands: map[uint64]string{}, maxReplay: maxReplay}
}

func (s *Session) Accept(epoch, clientSeq uint64, commandID string) CommandReceipt {
	if epoch != s.Epoch {
		return CommandReceipt{Disposition: CommandEpochMismatch, LastSeq: s.LastProcessedSeq}
	}
	if id, ok := s.commands[clientSeq]; ok {
		if id == commandID {
			return CommandReceipt{Disposition: CommandDuplicate, LastSeq: s.LastProcessedSeq}
		}
		return CommandReceipt{Disposition: CommandOutOfOrder, LastSeq: s.LastProcessedSeq}
	}
	if clientSeq != s.ExpectedClientSeq {
		return CommandReceipt{Disposition: CommandOutOfOrder, LastSeq: s.LastProcessedSeq}
	}
	s.commands[clientSeq] = commandID
	s.LastProcessedSeq = clientSeq
	s.ExpectedClientSeq++
	return CommandReceipt{Disposition: CommandAccepted, LastSeq: s.LastProcessedSeq}
}

func (s *Session) Disconnect(nowTick uint64) {
	s.Connected = false
	s.DisconnectedAtTick = nowTick
}

func (s *Session) Reconnect(epoch, lastAppliedServerSeq, nowTick uint64) (ResumeOutcome, []ServerEvent, error) {
	if epoch != s.Epoch {
		return ResumeSessionReplaced, nil, ErrEpochMismatch
	}
	if !s.Connected && nowTick > s.DisconnectedAtTick+ReconnectGraceTicks {
		return ResumeGraceExpired, nil, ErrGraceExpired
	}
	s.Connected = true
	if lastAppliedServerSeq == s.LastServerSeq {
		return ResumeDeltaReplay, nil, nil
	}
	if len(s.replay) == 0 || lastAppliedServerSeq < s.replay[0].Seq {
		return ResumeFullSnapshot, nil, nil
	}
	var out []ServerEvent
	for _, event := range s.replay {
		if event.Seq > lastAppliedServerSeq {
			out = append(out, event)
		}
	}
	return ResumeDeltaReplay, out, nil
}

func (s *Session) AppendServerEvent(tick uint64, data []byte) ServerEvent {
	s.LastServerSeq++
	return s.appendServerEvent(s.LastServerSeq, tick, data)
}

func (s *Session) AppendServerEventWithSeq(tick uint64, build func(uint64) ([]byte, error)) (ServerEvent, error) {
	next := s.LastServerSeq + 1
	data, err := build(next)
	if err != nil {
		return ServerEvent{}, err
	}
	s.LastServerSeq = next
	return s.appendServerEvent(next, tick, data), nil
}

func (s *Session) appendServerEvent(seq, tick uint64, data []byte) ServerEvent {
	event := ServerEvent{Seq: seq, Tick: tick, Data: append([]byte(nil), data...)}
	s.replay = append(s.replay, event)
	if len(s.replay) > s.maxReplay {
		copy(s.replay, s.replay[len(s.replay)-s.maxReplay:])
		s.replay = s.replay[:s.maxReplay]
	}
	return event
}

func (s *Session) AvatarTargetable(nowTick uint64) bool {
	return s.Connected || nowTick <= s.DisconnectedAtTick+ReconnectGraceTicks
}
