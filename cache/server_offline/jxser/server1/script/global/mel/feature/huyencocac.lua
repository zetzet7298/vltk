Include("\\script\\lib\\string.lua")
Include("\\script\\vng_lib\\files_lib.lua")

TB_ListToaDo_HuyenCoCac = {
    {nNpcId = 1605, nNpcLv = 95, fileName = "huyencocac.txt", nCount=1},
}

function tao_bai_train_ex_HuyenCoCac(nNpcId, nNpcLv, nCount, fileName,filePath)
    if fileName == nil then
        print("not file name to create training area "..nNpcLv)
        return
    end
    if filePath == nil then
        filePath = "settings/global/mel/"
    end
    local tbPattern = {"*n", "*n", "*n", "*n", "*n", "*l"}
    local tbData = tbVngLib_File:TableFromFile(filePath, fileName, tbPattern)
    if not tbData or getn(tbData) == 0 then
        print("No coordinate data found to create training area "..fileName)
        return
    end
	local nSeriesMonster
	local idmap
	local pX
	local pY
    local nameMonster = "Huy“n Tinh C¨ Quan"
    for i = 1, getn(tbData) do
        local row = tbData[i]
        idmap = row[1]
        pX = row[2]
        pY = row[3]
        if idmap and pX and pY then
            for j = 1, nCount do
                local isBoss = 2
				nSeriesMonster = random(0, 4)
                AddNpcEx(nNpcId, nNpcLv, nSeriesMonster, SubWorldID2Idx(idmap), (pX + random(-5,5)) * 32, (pY + random(-5,5)) * 32, 0, nameMonster, isBoss)
            end
        end
    end
end

function tao_bai_train_HuyenCoCac()
    for i =1, getn(TB_ListToaDo_HuyenCoCac) do
        local tbInfo = TB_ListToaDo_HuyenCoCac[i]
        tao_bai_train_ex_HuyenCoCac(tbInfo.nNpcId, tbInfo.nNpcLv, tbInfo.nCount, tbInfo.fileName)
    end
end