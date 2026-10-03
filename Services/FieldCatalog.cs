using System.Collections.Generic;

namespace RotdToolkit2.Services
{
    public sealed class FieldMeta
    {
        public string Label;
        public string Group;
        public bool Advanced;
    }

    /// <summary>Road of the Dead 2 存档字段的中文名称与分组，未知字段回退为原始名称。</summary>
    public static class FieldCatalog
    {
        private static readonly Dictionary<string, FieldMeta> Map = new Dictionary<string, FieldMeta>
        {
            // 资源与进度
            ["m_iRP"] = new FieldMeta { Label = "金钱 (RP)", Group = "资源与进度" },
            ["m_iStoryLevel"] = new FieldMeta { Label = "剧情关卡", Group = "资源与进度" },
            ["m_iHardcoreStoryLevel"] = new FieldMeta { Label = "硬核剧情关卡", Group = "资源与进度" },
            ["m_fStoryProgress"] = new FieldMeta { Label = "剧情进度", Group = "资源与进度" },
            ["m_iStoryTutorialIndex"] = new FieldMeta { Label = "剧情教学进度", Group = "资源与进度" },
            ["m_bIsAwareOfStoryMode"] = new FieldMeta { Label = "已解锁剧情模式", Group = "资源与进度" },
            ["m_bStoryModeFinished"] = new FieldMeta { Label = "剧情已通关", Group = "资源与进度" },
            ["m_bMutantLevelFinished"] = new FieldMeta { Label = "变异模式已通关", Group = "资源与进度" },
            ["m_bJohnEndingSeen"] = new FieldMeta { Label = "看过 John 结局", Group = "资源与进度" },
            ["m_bSeenJohnCarNotice"] = new FieldMeta { Label = "看过 John 车辆提示", Group = "资源与进度" },
            ["m_bSeenMutantModeNotice"] = new FieldMeta { Label = "看过变异模式提示", Group = "资源与进度" },
            ["m_bSeenChallengeModeNotice"] = new FieldMeta { Label = "看过挑战模式提示", Group = "资源与进度" },
            ["m_bBeenInGarage"] = new FieldMeta { Label = "到过车库", Group = "资源与进度" },
            ["m_bSavedOnce"] = new FieldMeta { Label = "已保存过(内部)", Group = "资源与进度", Advanced = true },

            // 角色与载具
            ["m_iDianeDrivingLevel"] = new FieldMeta { Label = "Diane 驾驶等级", Group = "角色与载具" },
            ["m_iCochetaDrivingLevel"] = new FieldMeta { Label = "Cocheta 驾驶等级", Group = "角色与载具" },
            ["m_iDianeDrivingPoints"] = new FieldMeta { Label = "Diane 驾驶点数", Group = "角色与载具" },
            ["m_iCochetaDrivingPoints"] = new FieldMeta { Label = "Cocheta 驾驶点数", Group = "角色与载具" },
            ["m_iDianeToughnessLevel"] = new FieldMeta { Label = "Diane 韧性等级", Group = "角色与载具" },
            ["m_iCochetaToughnessLevel"] = new FieldMeta { Label = "Cocheta 韧性等级", Group = "角色与载具" },
            ["m_iDianeToughnessPoints"] = new FieldMeta { Label = "Diane 韧性点数", Group = "角色与载具" },
            ["m_iCochetaToughnessPoints"] = new FieldMeta { Label = "Cocheta 韧性点数", Group = "角色与载具" },
            ["m_iTiresLevel"] = new FieldMeta { Label = "轮胎 等级", Group = "角色与载具" },
            ["m_fNbBlownTires"] = new FieldMeta { Label = "爆胎数", Group = "角色与载具" },
            ["m_fBodyHP"] = new FieldMeta { Label = "车体生命值", Group = "角色与载具" },
            ["m_iBumperLevel"] = new FieldMeta { Label = "保险杠 等级", Group = "角色与载具" },
            ["m_fBumperHP"] = new FieldMeta { Label = "保险杠生命值", Group = "角色与载具" },
            ["m_fBumperAddonHP"] = new FieldMeta { Label = "保险杠附加生命值", Group = "角色与载具" },
            ["m_iEngineLevel"] = new FieldMeta { Label = "引擎 等级", Group = "角色与载具" },
            ["m_iMedShields"] = new FieldMeta { Label = "医疗护盾数", Group = "角色与载具" },
            ["m_iMedHeals"] = new FieldMeta { Label = "医疗治疗数", Group = "角色与载具" },
            ["m_iChopperSupportLevel"] = new FieldMeta { Label = "直升机支援 等级", Group = "角色与载具" },
            ["m_iAmmoPickupLevel"] = new FieldMeta { Label = "弹药拾取 等级", Group = "角色与载具" },
            ["m_iLowerCostLevel"] = new FieldMeta { Label = "降低消耗 等级", Group = "角色与载具" },
            ["m_iDriverSwitchLevel"] = new FieldMeta { Label = "切换驾驶员 等级", Group = "角色与载具" },
            ["m_iTaskSpeedLevel"] = new FieldMeta { Label = "任务速度 等级", Group = "角色与载具" },
            ["m_iLevelUpLevel"] = new FieldMeta { Label = "升级 等级", Group = "角色与载具" },
            ["m_bSavedAmmo"] = new FieldMeta { Label = "已保存弹药", Group = "角色与载具" },
            ["m_bUsingHummer"] = new FieldMeta { Label = "使用悍马", Group = "角色与载具" },
            ["m_iCivsInCar"] = new FieldMeta { Label = "车内平民数", Group = "角色与载具" },
            ["m_bCivIsMale1"] = new FieldMeta { Label = "平民 1 为男性", Group = "角色与载具" },
            ["m_bCivIsMale2"] = new FieldMeta { Label = "平民 2 为男性", Group = "角色与载具" },
            ["m_bCivIsMale3"] = new FieldMeta { Label = "平民 3 为男性", Group = "角色与载具" },
            ["m_bCivIsMale4"] = new FieldMeta { Label = "平民 4 为男性", Group = "角色与载具" },
            ["m_bCivIsMale5"] = new FieldMeta { Label = "平民 5 为男性", Group = "角色与载具" },

            // 武器
            ["m_Weapons"] = new FieldMeta { Label = "武器数据 (含各武器改装)", Group = "武器" },

            // 成绩与统计
            ["m_fBestDeadOnTimeDistance"] = new FieldMeta { Label = "最佳 Dead on Time 距离", Group = "成绩与统计" },
            ["m_fBestSurvivalDistance"] = new FieldMeta { Label = "最佳生存距离", Group = "成绩与统计" },
            ["m_fBestMilitaryDistance"] = new FieldMeta { Label = "最佳军事距离", Group = "成绩与统计" },
            ["m_fBestMutantDistance"] = new FieldMeta { Label = "最佳变异距离", Group = "成绩与统计" },
            ["m_iBestChopperRescueRescues"] = new FieldMeta { Label = "最佳直升机救援数", Group = "成绩与统计" },
            ["m_iBestHitCombo"] = new FieldMeta { Label = "最佳连击", Group = "成绩与统计" },
            ["m_fTotalDistance"] = new FieldMeta { Label = "总行驶距离", Group = "成绩与统计" },
            ["m_fDianeDistance"] = new FieldMeta { Label = "Diane 行驶距离", Group = "成绩与统计" },
            ["m_fCochetaDistance"] = new FieldMeta { Label = "Cocheta 行驶距离", Group = "成绩与统计" },
            ["m_iPressSwerveCount"] = new FieldMeta { Label = "按闪避次数", Group = "成绩与统计" },
            ["m_iNbZombieSplatterHits"] = new FieldMeta { Label = "撞碎丧尸", Group = "成绩与统计" },
            ["m_iNbKilledCivilians"] = new FieldMeta { Label = "击杀平民", Group = "成绩与统计" },
            ["m_iNbDodgedBombing"] = new FieldMeta { Label = "躲避轰炸", Group = "成绩与统计" },
            ["m_iZombiesShot"] = new FieldMeta { Label = "射击丧尸数", Group = "成绩与统计" },
            ["m_iZombiesHit"] = new FieldMeta { Label = "命中丧尸数", Group = "成绩与统计" },
            ["m_iMutantsKilled"] = new FieldMeta { Label = "击杀变异体", Group = "成绩与统计" },
            ["m_iBulletsShot"] = new FieldMeta { Label = "发射子弹数", Group = "成绩与统计" },
            ["m_iRocketsShot"] = new FieldMeta { Label = "发射火箭数", Group = "成绩与统计" },
            ["m_iCarsExploded"] = new FieldMeta { Label = "炸毁车辆数", Group = "成绩与统计" },
            ["m_iCiviliansSaved"] = new FieldMeta { Label = "拯救平民数", Group = "成绩与统计" },
            ["m_iCiviliansTaken"] = new FieldMeta { Label = "接载平民数", Group = "成绩与统计" },
            ["m_iRPEarned"] = new FieldMeta { Label = "累计获得 RP", Group = "成绩与统计" },
            ["m_iSuccessfulStops"] = new FieldMeta { Label = "成功停车数", Group = "成绩与统计" },
            ["m_iHeadshots"] = new FieldMeta { Label = "爆头数", Group = "成绩与统计" },
            ["m_iMinesShot"] = new FieldMeta { Label = "射击地雷数", Group = "成绩与统计" },
            ["m_iFuelTruckShot"] = new FieldMeta { Label = "射击油罐车数", Group = "成绩与统计" },
            ["m_iPropertyDamage"] = new FieldMeta { Label = "财产破坏", Group = "成绩与统计" },
            ["m_iTotalDeaths"] = new FieldMeta { Label = "总死亡次数", Group = "成绩与统计" },
            ["m_iDeathsByZombie"] = new FieldMeta { Label = "被丧尸击杀", Group = "成绩与统计" },
            ["m_iDeathsByExplosion"] = new FieldMeta { Label = "被爆炸击杀", Group = "成绩与统计" },
            ["m_iDeathsByGunShot"] = new FieldMeta { Label = "被枪击杀", Group = "成绩与统计" },
            ["m_iCollateralKills"] = new FieldMeta { Label = "附带击杀", Group = "成绩与统计" },
            ["m_iFiresPutOut"] = new FieldMeta { Label = "扑灭火焰", Group = "成绩与统计" },
            ["m_iTotalClingers"] = new FieldMeta { Label = "攀附者总数", Group = "成绩与统计" },
            ["m_iClingersShot"] = new FieldMeta { Label = "射击攀附者", Group = "成绩与统计" },
            ["m_iClingersShakenOff"] = new FieldMeta { Label = "甩脱攀附者", Group = "成绩与统计" },
            ["m_iSoldiersHit"] = new FieldMeta { Label = "命中士兵数", Group = "成绩与统计" },
            ["m_iSoldiersShot"] = new FieldMeta { Label = "射击士兵数", Group = "成绩与统计" },
            ["m_iCarsBlownUpByMountedWeapon"] = new FieldMeta { Label = "用载具武器炸毁车辆", Group = "成绩与统计" },
            ["m_iNbSlowZombieKills"] = new FieldMeta { Label = "慢速丧尸击杀", Group = "成绩与统计" },
            ["m_iTiresBlown"] = new FieldMeta { Label = "轮胎打爆数", Group = "成绩与统计" },

            // 击败记录
            ["m_bKilledZombieMale"] = new FieldMeta { Label = "击败 男丧尸", Group = "击败记录" },
            ["m_bKilledZombieFemale"] = new FieldMeta { Label = "击败 女丧尸", Group = "击败记录" },
            ["m_bKilledZombieMaleNew"] = new FieldMeta { Label = "击败 男丧尸(新)", Group = "击败记录" },
            ["m_bKilledZombieFemaleNew"] = new FieldMeta { Label = "击败 女丧尸(新)", Group = "击败记录" },
            ["m_bKilledZombieHard"] = new FieldMeta { Label = "击败 困难丧尸", Group = "击败记录" },
            ["m_bKilledZombieSoldier"] = new FieldMeta { Label = "击败 士兵丧尸", Group = "击败记录" },
            ["m_bKilledZombieMutant"] = new FieldMeta { Label = "击败 变异丧尸", Group = "击败记录" },
            ["m_bKilledZombieAlpha"] = new FieldMeta { Label = "击败 阿尔法丧尸", Group = "击败记录" },
            ["m_bKilledZombieGrub"] = new FieldMeta { Label = "击败 幼虫丧尸", Group = "击败记录" },
            ["m_bKilledZombieRanger"] = new FieldMeta { Label = "击败 游骑兵丧尸", Group = "击败记录" },
            ["m_bKilledSoldier"] = new FieldMeta { Label = "击败 士兵", Group = "击败记录" },

            // 见闻与教学
            ["m_bSeenAccelTutorial"] = new FieldMeta { Label = "加速教学已显示", Group = "见闻与教学" },
            ["m_bPressedAccel"] = new FieldMeta { Label = "按过加速", Group = "见闻与教学" },
            ["m_bPressedBrake"] = new FieldMeta { Label = "按过刹车", Group = "见闻与教学" },
            ["m_bSeenTurnTutorial"] = new FieldMeta { Label = "转向教学已显示", Group = "见闻与教学" },
            ["m_bPressedLeft"] = new FieldMeta { Label = "按过左转", Group = "见闻与教学" },
            ["m_bPressedRight"] = new FieldMeta { Label = "按过右转", Group = "见闻与教学" },
            ["m_bSeenMouseTutorial"] = new FieldMeta { Label = "鼠标教学已显示", Group = "见闻与教学" },
            ["m_bPressedShoot"] = new FieldMeta { Label = "按过射击", Group = "见闻与教学" },
            ["m_bSeenReloadTutorial"] = new FieldMeta { Label = "装弹教学已显示", Group = "见闻与教学" },
            ["m_bPressedReload"] = new FieldMeta { Label = "按过装弹", Group = "见闻与教学" },
            ["m_bSeenWeaponSelectionTutorial"] = new FieldMeta { Label = "武器选择教学已显示", Group = "见闻与教学" },
            ["m_bPressedWeaponSelection"] = new FieldMeta { Label = "按过武器选择", Group = "见闻与教学" },
            ["m_bSeenWeaponCycleTutorial"] = new FieldMeta { Label = "武器切换教学已显示", Group = "见闻与教学" },
            ["m_bPressedWeaponCycle"] = new FieldMeta { Label = "按过武器切换", Group = "见闻与教学" },
            ["m_bSeenSwerveTutorial"] = new FieldMeta { Label = "闪避教学已显示", Group = "见闻与教学" },
            ["m_bPressedSwerve"] = new FieldMeta { Label = "按过闪避", Group = "见闻与教学" },
            ["m_bSeenHornTutorial"] = new FieldMeta { Label = "喇叭教学已显示", Group = "见闻与教学" },
            ["m_bPressedHorn"] = new FieldMeta { Label = "按过喇叭", Group = "见闻与教学" },
            ["m_bSeenMountedWeaponTutorial"] = new FieldMeta { Label = "载具武器教学已显示", Group = "见闻与教学" },
            ["m_bPressedMountedWeapon"] = new FieldMeta { Label = "按过载具武器", Group = "见闻与教学" },
            ["m_bSeenThirdWeaponTutorial"] = new FieldMeta { Label = "第三武器教学已显示", Group = "见闻与教学" },
            ["m_bPressedThirdWeapon"] = new FieldMeta { Label = "按过第三武器", Group = "见闻与教学" },
            ["m_bSeenFourthWeaponTutorial"] = new FieldMeta { Label = "第四武器教学已显示", Group = "见闻与教学" },
            ["m_bPressedFourthWeapon"] = new FieldMeta { Label = "按过第四武器", Group = "见闻与教学" },
            ["m_bSeenInteractTutorial"] = new FieldMeta { Label = "互动教学已显示", Group = "见闻与教学" },
            ["m_bPressedInteract"] = new FieldMeta { Label = "按过互动", Group = "见闻与教学" },
            ["m_bSeenDropCivTutorial"] = new FieldMeta { Label = "放下平民教学已显示", Group = "见闻与教学" },
            ["m_iSeenSideGrappleDriverTutorial"] = new FieldMeta { Label = "侧面擒抱(驾驶)教学", Group = "见闻与教学" },
            ["m_iSeenSideGrappleGunnerTutorial"] = new FieldMeta { Label = "侧面擒抱(炮手)教学", Group = "见闻与教学" },
            ["m_bSeenLocationTutorial"] = new FieldMeta { Label = "地点教学已显示", Group = "见闻与教学" },
            ["m_bStoppedAtLocation"] = new FieldMeta { Label = "曾在某地点停车", Group = "见闻与教学" },
            ["m_bSeenMedTutorial"] = new FieldMeta { Label = "医疗教学已显示", Group = "见闻与教学" },
            ["m_bSeenChopperTutorial"] = new FieldMeta { Label = "直升机教学已显示", Group = "见闻与教学" },
            ["m_bSeenSwitchDriverTutorial"] = new FieldMeta { Label = "切换驾驶员教学已显示", Group = "见闻与教学" },
            ["m_bSeenUpgradeSystemTutorial"] = new FieldMeta { Label = "升级系统教学已显示", Group = "见闻与教学" },
            ["m_bPressedSwitchDriver"] = new FieldMeta { Label = "按过切换驾驶员", Group = "见闻与教学" },
            ["m_bSeenExtraSwerveTutorial"] = new FieldMeta { Label = "额外闪避教学已显示", Group = "见闻与教学" },
            ["m_bSeenExtraLocationTutorial"] = new FieldMeta { Label = "额外地点教学已显示", Group = "见闻与教学" },
            ["m_bSeenExtraExtraLocationTutorial"] = new FieldMeta { Label = "额外地点教学 2 已显示", Group = "见闻与教学" },
            ["m_bSeenAreaResidential"] = new FieldMeta { Label = "到过住宅区", Group = "见闻与教学" },
            ["m_bSeenAreaTown"] = new FieldMeta { Label = "到过城镇", Group = "见闻与教学" },
            ["m_bSeenAreaIndustrial"] = new FieldMeta { Label = "到过工业区", Group = "见闻与教学" },
            ["m_bSeenAreaPark"] = new FieldMeta { Label = "到过公园", Group = "见闻与教学" },
            ["m_bSeenAreaAlleyway"] = new FieldMeta { Label = "到过小巷", Group = "见闻与教学" },
            ["m_bSeenAreaHighway"] = new FieldMeta { Label = "到过高速公路", Group = "见闻与教学" },
            ["m_bSeenAreaCemetery"] = new FieldMeta { Label = "到过墓地", Group = "见闻与教学" },

            // 收集与成就
            ["m_completedAchievements"] = new FieldMeta { Label = "已完成成就", Group = "收集与成就" },
            ["m_SurvivalItems"] = new FieldMeta { Label = "生存模式解锁项", Group = "收集与成就" },
            ["m_SurvivalItemsSeen"] = new FieldMeta { Label = "生存模式已见项", Group = "收集与成就" },

            // 按键
            ["m_iLeftKey"] = new FieldMeta { Label = "按键：左转", Group = "按键" },
            ["m_iRightKey"] = new FieldMeta { Label = "按键：右转", Group = "按键" },
            ["m_iAccelKey"] = new FieldMeta { Label = "按键：加速", Group = "按键" },
            ["m_iBrakeKey"] = new FieldMeta { Label = "按键：刹车", Group = "按键" },
            ["m_iHandbrakeKey"] = new FieldMeta { Label = "按键：手刹", Group = "按键" },
            ["m_iHornKey"] = new FieldMeta { Label = "按键：喇叭", Group = "按键" },
            ["m_iSwitchDriverKey"] = new FieldMeta { Label = "按键：切换驾驶员", Group = "按键" },
            ["m_iReloadKey"] = new FieldMeta { Label = "按键：装弹", Group = "按键" },
            ["m_iPrevWeaponKey"] = new FieldMeta { Label = "按键：上一武器", Group = "按键" },
            ["m_iNextWeaponKey"] = new FieldMeta { Label = "按键：下一武器", Group = "按键" },
            ["m_iPauseKey"] = new FieldMeta { Label = "按键：暂停", Group = "按键" },

            // 设置
            ["m_iQuality"] = new FieldMeta { Label = "画质", Group = "设置" },
            ["m_bMusic"] = new FieldMeta { Label = "音乐", Group = "设置" },
            ["m_bSound"] = new FieldMeta { Label = "音效", Group = "设置" },
            ["m_bZombieTints"] = new FieldMeta { Label = "丧尸着色", Group = "设置" },
            ["m_bShowWeaponStatsButton"] = new FieldMeta { Label = "显示武器数据按钮", Group = "设置" },
            ["m_bSeenUpgradeTabs"] = new FieldMeta { Label = "看过升级标签", Group = "设置" },
            ["m_bSeenStorageNotice"] = new FieldMeta { Label = "看过仓库提示", Group = "设置" },
            ["m_bDidFirstHeadShot"] = new FieldMeta { Label = "完成首次爆头", Group = "设置" },
            ["m_bSeenEditorTutorial"] = new FieldMeta { Label = "看过编辑器教学", Group = "设置" },
            ["m_bSeenApplyTutorial"] = new FieldMeta { Label = "看过应用教学", Group = "设置" },
            ["m_bSeenLoadCampaignHint"] = new FieldMeta { Label = "看过载入战役提示", Group = "设置" },
            ["m_bSeenStoryCampaignHint"] = new FieldMeta { Label = "看过剧情战役提示", Group = "设置" },
            ["m_bSeenChallengeCampaignHint"] = new FieldMeta { Label = "看过挑战战役提示", Group = "设置" }
        };

        public static FieldMeta Get(string name)
        {
            return Map.TryGetValue(name, out var meta) ? meta : new FieldMeta { Label = name, Group = "其他" };
        }

        public static string GroupOrder(string group)
        {
            switch (group)
            {
                case "资源与进度": return "1";
                case "角色与载具": return "2";
                case "武器": return "3";
                case "成绩与统计": return "4";
                case "击败记录": return "5";
                case "见闻与教学": return "6";
                case "收集与成就": return "7";
                case "按键": return "8";
                case "设置": return "9";
                default: return "99";
            }
        }
    }
}
