function main()
	if GetFightState() ~= 1 then
		SetPos(1592, 3206)
		SetFightState(1)
	else
		SetPos(1578, 3221)
		SetFightState(0)
	end
end