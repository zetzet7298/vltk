----------------------------------------------------------------------------------------------------
--								 Danh S¸ch Cöa Hµng Héi Qu¸n Vâ L©m								  --
----------------------------------------------------------------------------------------------------
--                          ID: 191 - Cöa hµng TiÒn V¹n - Ng©n Linh Nhi                           --
--                          ID: 192 - Cöa hµng Phóc Duyªn - Hång Duyªn Muéi                       --
--                          ID: 193 - Cöa hµng Vinh Dù - Ngù Vinh ThÇn                            --
--                          ID: 194 - Cöa hµng Tèng Kim - ThiÕt HuyÕt T­íng                       --
--                          ID: 195 - Cöa hµng ChiÕn M· - ThiÕt M· Hïng                           --
--                          ID: 196 - Cöa hµng BÝ KÝp - V¹n Ph¸p V« Danh						  --
----------------------------------------------------------------------------------------------------
function CuaHangNganLuong()
	Sale(191, 1)
end

function CuaHangPhucDuyen()
	Sale(192, 2)
end

function CuaHangVinhDu()
	Sale(193, 11)
end

function CuaHangTongKim()
	Sale(194, 4)
end

function CuaHangChienMa()
	Sale(195, 1)
end

function CuaHangBiKip()
	Sale(196, 1)
end

----------------------------------------------------------------------------------------------------
-- Test Server
function CuaHangTuyetDinh()
	Sale(207, 1)
end

function CuaHangThuNghiem()
	CreateStores()
		AddShop2Stores(200, "Phi Phong", 1, 100, "1")
		AddShop2Stores(201, "Ên", 1, 100, "1")
		AddShop2Stores(202, "Trang Søc", 1, 100, "1")
		AddShop2Stores(203, "MÆt M¹", 1, 100, "1")
		AddShop2Stores(204, "Ngùa", 1, 100, "1")
		AddShop2Stores(205, "VËt PhÈm", 1, 100, "1")
	OpenStores()
end
----------------------------------------------------------------------------------------------------