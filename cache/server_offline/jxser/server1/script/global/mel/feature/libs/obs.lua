function GetObstacles(tbPos)
	local x = tbPos[1]
	local y = tbPos[2]
	local nNum = tbPos[3]
	local step = tbPos[4]
	local direction = tbPos[5]
    if step == nil then step = 32 end
    if direction == nil then direction = 1 end
    local coords = {}
    local count = 0
    local curX, curY = x, y
    while count < nNum do
        curX = curX + (direction * step)
        for j = 1, 1 do
            if count < nNum then
                tinsert(coords, {curX, curY + j*step})
                count = count + 1
            end
        end

        if count >= nNum then break end
        curY = curY + step
        for j = 1, 1 do
            if count < nNum then
                tinsert(coords, {curX, curY + j*step})
                count = count + 1
            end
        end
    end
    return coords
end