Include("\\script\\lib\\string.lua")
Include("\\script\\vng_lib\\files_lib.lua")

TB_ListToaDo = {
    -- Tu Luy÷n CËc 10-180
    {nNpcId = 12,   nNpcLv = 15, fileName = "tuluyencap10.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 10"},
    {nNpcId = 6,    nNpcLv = 25, fileName = "tuluyencap20.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 20"},
    {nNpcId = 1,    nNpcLv = 35, fileName = "tuluyencap30.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 30"},
    {nNpcId = 10,   nNpcLv = 45, fileName = "tuluyencap40.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 40"},
    {nNpcId = 15,   nNpcLv = 55, fileName = "tuluyencap50.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 50"},
    {nNpcId = 17,   nNpcLv = 65, fileName = "tuluyencap60.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 60"},
    {nNpcId = 18,   nNpcLv = 75, fileName = "tuluyencap70.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 70"},
    {nNpcId = 13,   nNpcLv = 85, fileName = "tuluyencap80.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 80"},
    {nNpcId = 600,  nNpcLv = 95, fileName = "tuluyencap90.txt",  nCount=20, sMonsterName = "Tu Luy÷n Qu∏i 90"},
    {nNpcId = 600,  nNpcLv = 95, fileName = "tuluyencap100.txt", nCount=15, sMonsterName = "Tu Luy÷n Qu∏i 100"},
    {nNpcId = 1521, nNpcLv = 95, fileName = "tuluyencap120.txt", nCount=15, sMonsterName = "Tu Luy÷n Qu∏i 120"},
    {nNpcId = 1522, nNpcLv = 95, fileName = "tuluyencap140.txt", nCount=15, sMonsterName = "Tu Luy÷n Qu∏i 140"},
    {nNpcId = 1523, nNpcLv = 95, fileName = "tuluyencap160.txt", nCount=15, sMonsterName = "Tu Luy÷n Qu∏i 160"},
    {nNpcId = 1524, nNpcLv = 95, fileName = "tuluyencap180.txt", nCount=15, sMonsterName = "Tu Luy÷n Qu∏i 180"},

    -- D∑ T»u 90
    {nNpcId = 597,  nNpcLv = 95, fileName = "truongbachbac.txt", nCount=12, sMonsterName = "ßoπt Ph∏ch"},
    {nNpcId = 600,  nNpcLv = 95, fileName = "truongbachnam.txt", nCount=12, sMonsterName = "Gi∏ng ChÔy"},
    {nNpcId = 156,  nNpcLv = 95, fileName = "khoalangdong.txt",  nCount=12, sMonsterName = "∂nh C´n"},
    {nNpcId = 560,  nNpcLv = 95, fileName = "samac1.txt",        nCount=12, sMonsterName = "L≠u Quang"},
    {nNpcId = 559,  nNpcLv = 95, fileName = "samac2.txt",        nCount=12, sMonsterName = "L≠u Phong"},
    {nNpcId = 534,  nNpcLv = 95, fileName = "samac3.txt",        nCount=12, sMonsterName = "TÀt VÚ"},
    {nNpcId = 142,  nNpcLv = 95, fileName = "duocvuong4.txt",    nCount=12, sMonsterName = "ßao Kh∏ch"},
    {nNpcId = 146,  nNpcLv = 95, fileName = "tiencucdong.txt",   nCount=12, sMonsterName = "ßoπt m÷nh Li™m"},
    {nNpcId = 14,   nNpcLv = 95, fileName = "canviendong.txt",   nCount=12, sMonsterName = "Voi Hoµng Hµ"},
}

function tao_bai_train_ex(nNpcId, nNpcLv, nCount, fileName, szMonsterName, filePath)
    if fileName == nil then
        print("not file name to create training area "..nNpcLv)
        return
    end
    if filePath == nil then
        filePath = "settings/global/mel/"
    end
    local szName = szMonsterName
    local tbPattern = {"*n", "*n", "*n", "*n", "*n", "*l"}
    local tbData = tbVngLib_File:TableFromFile(filePath, fileName, tbPattern)
    if not tbData or getn(tbData) == 0 then
        print("No coordinate data found to create training area "..fileName)
        return
    end
    for i = 1, getn(tbData) do
        local row = tbData[i]
        local idmap = row[1]
        local pX = row[2]
        local pY = row[3]
        if idmap and pX and pY then
            for j = 1, nCount do
                local isBoss = 0
                if (mod(j, 10) == 0) then 
                    isBoss = 2
                end
                local nSeriesMonster = random(0, 4)
                AddNpcEx(nNpcId, nNpcLv, nSeriesMonster, SubWorldID2Idx(idmap), (pX + random(-5,5)) * 32, (pY + random(-5,5)) * 32, 0, szName, isBoss)
            end
        end
    end
end

function tao_bai_train()
    for i = 1, getn(TB_ListToaDo) do
        local tbInfo = TB_ListToaDo[i]
        tao_bai_train_ex(tbInfo.nNpcId, tbInfo.nNpcLv, tbInfo.nCount, tbInfo.fileName, tbInfo.sMonsterName)
    end
end