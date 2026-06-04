using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace StoreAndInventory
{
    /// <summary>
    /// 商店主面板控制器。
    /// 左侧分类筛选（全部/物品/符文/收藏品）+ 顶部效果筛选（AP/MP/HP/KO）+ 右侧详情 + 购买数量控制。
    /// 适配 ExploreScene 中 ShopPanel 的节点命名。
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        // ── 服务依赖 ──
        [SerializeField] ShopService shopService;
        [SerializeField] WalletService wallet;
        [SerializeField] Inventory inventory;

        /// <summary>商店面板关闭时触发。</summary>
        public event System.Action Closed;

        // ── 左侧分类按钮（全部 / 物品 / 收藏品 / 预留） ──
        [Header("左侧分类按钮")]
        [SerializeField] Button[] leftBtns;
        [SerializeField] Image[] leftBtnBgs;   // 每个按钮的 bg 子物体
        [SerializeField] Text[] leftBtnTxts;

        // ── 顶部筛选按钮（AP / MP / HP / KO） ──
        [Header("顶部筛选按钮")]
        [SerializeField] Button[] topBtns;
        [SerializeField] Image[] topBtnBgs;
        [SerializeField] Text[] topBtnTxts;

        // ── 物品列表 ──
        [Header("物品列表")]
        [SerializeField] Transform content;          // ScrollView > Viewport > Content
        [SerializeField] ShopItemSlot itemSlotPrefab;

        // ── 右侧详情面板 ──
        [Header("右侧详情面板")]
        [SerializeField] GameObject rightDetailPanel;
        [SerializeField] Text txtName;
        [SerializeField] Text txtHaveNum;
        [SerializeField] Text txtIntro;
        [SerializeField] Text txtEffDesc;
        [SerializeField] Text txtBuyNum;
        [SerializeField] Image icon;
        [SerializeField] Image detailCurrencyIcon;
        [SerializeField] Sprite detailInkSprite;
        [SerializeField] Sprite detailSpiritSprite;

        [Header("关闭按钮")]
        [SerializeField] Button closeButton;

        [Header("购买数量按钮")]
        [SerializeField] Button btnDec;
        [SerializeField] Button btnMin;
        [SerializeField] Button btnInc;
        [SerializeField] Button btnMax;
        [SerializeField] Button btnBuy;
        [SerializeField] Text txtBuyPrice;  // 购买按钮上的价格文本

        // ── 货币显示 ──
        [Header("货币显示")]
        [SerializeField] Text txtInk;
        [SerializeField] Text txtSpirit;

        [Header("按钮颜色")]
        [SerializeField] Color selectedTextColor = Color.white;
        [SerializeField] Color normalTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        // ── 内部状态 ──
        int selectedLeftIndex;   // 0=全部 1=物品 2=收藏品 3=预留
        int selectedTopIndex = -1; // -1=不筛选 0=AP 1=MP 2=HP 3=KO
        ShopEntry selectedEntry;
        int buyCount = 1;
        readonly List<ShopItemSlot> spawnedSlots = new();
        readonly StringBuilder _sb = new(128);

        // ── 缓存委托（用于正确取消订阅） ──
        System.Action refreshAction;

        void Awake()
        {
            if (shopService == null) shopService = FindObjectOfType<ShopService>();
            if (wallet == null) wallet = FindObjectOfType<WalletService>();
            if (inventory == null) inventory = FindObjectOfType<Inventory>();
        }

        void OnEnable()
        {
            refreshAction = () => Refresh();

            // 自动配置滚动布局（确保 ContentSizeFitter 存在）
            EnsureScrollLayout();

            if (shopService != null)
            {
                shopService.OnShopOpened += _ => Refresh();
                shopService.OnShopChanged += refreshAction;
            }
            if (wallet != null) wallet.OnChanged += HandleWalletChanged;
            if (inventory != null) inventory.OnChanged += refreshAction;

            for (var i = 0; i < leftBtns.Length; i++)
            {
                var idx = i;
                if (leftBtns[i] != null)
                    leftBtns[i].onClick.AddListener(() => OnLeftBtnClick(idx));
            }

            for (var i = 0; i < topBtns.Length; i++)
            {
                var idx = i;
                if (topBtns[i] != null)
                    topBtns[i].onClick.AddListener(() => OnTopBtnClick(idx));
            }

            if (btnDec != null) btnDec.onClick.AddListener(() => AdjustBuyCount(-1));
            if (btnMin != null) btnMin.onClick.AddListener(() => SetBuyCount(1));
            if (btnInc != null) btnInc.onClick.AddListener(() => AdjustBuyCount(1));
            if (btnMax != null) btnMax.onClick.AddListener(SetBuyCountMax);
            if (btnBuy != null) btnBuy.onClick.AddListener(OnClickBuy);
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            EnsureShopOpen();
            selectedLeftIndex = 0;
            selectedTopIndex = -1;
            UpdateLeftBtnVisuals();
            UpdateTopBtnVisuals();
            RefreshInk();
            Refresh();
        }

        void OnDisable()
        {
            if (shopService != null)
            {
                shopService.OnShopOpened -= _ => Refresh();
                if (refreshAction != null) shopService.OnShopChanged -= refreshAction;
            }
            if (wallet != null) wallet.OnChanged -= HandleWalletChanged;
            if (inventory != null && refreshAction != null) inventory.OnChanged -= refreshAction;

            for (var i = 0; i < leftBtns.Length; i++)
                leftBtns[i]?.onClick.RemoveAllListeners();
            for (var i = 0; i < topBtns.Length; i++)
                topBtns[i]?.onClick.RemoveAllListeners();

            if (btnDec != null) btnDec.onClick.RemoveAllListeners();
            if (btnMin != null) btnMin.onClick.RemoveAllListeners();
            if (btnInc != null) btnInc.onClick.RemoveAllListeners();
            if (btnMax != null) btnMax.onClick.RemoveAllListeners();
            if (btnBuy != null) btnBuy.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();

            ClearSelection();
        }

        // ── 公共 API ──

        public bool IsOpen => gameObject.activeSelf;

        public void Open()
        {
            EnsureShopOpen();
            gameObject.SetActive(true);
            RefreshInk();
            Refresh();
        }

        public void Close()
        {
            ClearSelection();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        // ── 左侧按钮 ──

        void OnLeftBtnClick(int index)
        {
            selectedLeftIndex = index;
            selectedTopIndex = -1;
            UpdateLeftBtnVisuals();
            UpdateTopBtnVisuals();
            ClearSelection();
            Refresh();
        }

        // ── 顶部按钮 ──

        void OnTopBtnClick(int index)
        {
            selectedTopIndex = selectedTopIndex == index ? -1 : index;
            UpdateTopBtnVisuals();
            ClearSelection();
            Refresh();
        }

        // ── 按钮视觉 ──

        void UpdateLeftBtnVisuals()
        {
            for (var i = 0; i < leftBtns.Length; i++)
            {
                var active = i == selectedLeftIndex;
                if (i < leftBtnBgs.Length && leftBtnBgs[i] != null)
                    leftBtnBgs[i].enabled = active;
                if (i < leftBtnTxts.Length && leftBtnTxts[i] != null)
                    leftBtnTxts[i].color = active ? selectedTextColor : normalTextColor;
            }
        }

        void UpdateTopBtnVisuals()
        {
            for (var i = 0; i < topBtns.Length; i++)
            {
                var active = i == selectedTopIndex;
                if (i < topBtnBgs.Length && topBtnBgs[i] != null)
                    topBtnBgs[i].enabled = active;
                if (i < topBtnTxts.Length && topBtnTxts[i] != null)
                    topBtnTxts[i].color = active ? selectedTextColor : normalTextColor;
            }
        }

        // ── 刷新列表 ──

        void Refresh(string reselectItemId = null)
        {
            ClearSlots();
            if (rightDetailPanel != null) rightDetailPanel.SetActive(false);

            if (shopService == null || !shopService.IsOpen || itemSlotPrefab == null)
                return;

            var entries = shopService.GetVisibleEntries();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry?.item == null) continue;
                if (!PassesFilter(entry.item)) continue;

                var slot = Instantiate(itemSlotPrefab, content);
                var price = shopService.GetPrice(entry);
                var stock = shopService.GetRemainingStock(entry.item.id);
                slot.Bind(entry, stock, price);
                slot.OnSelected += HandleSlotSelected;
                spawnedSlots.Add(slot);
            }

            // 尝试重新选中之前的物品
            if (!string.IsNullOrEmpty(reselectItemId) && spawnedSlots.Count > 0)
            {
                for (var i = 0; i < spawnedSlots.Count; i++)
                {
                    if (spawnedSlots[i].Entry?.item != null && spawnedSlots[i].Entry.item.id == reselectItemId)
                    {
                        HandleSlotSelected(spawnedSlots[i].Entry);
                        return;
                    }
                }
            }

            // 默认选中第一个物品
            if (spawnedSlots.Count > 0)
                HandleSlotSelected(spawnedSlots[0].Entry);
        }

        void ClearSlots()
        {
            for (var i = spawnedSlots.Count - 1; i >= 0; i--)
            {
                if (spawnedSlots[i] != null)
                    Destroy(spawnedSlots[i].gameObject);
            }
            spawnedSlots.Clear();
        }

        // ── 筛选逻辑 ──

        bool PassesFilter(ItemBase item)
        {
            if (!PassesLeftFilter(item)) return false;
            if (!PassesTopFilter(item)) return false;
            return true;
        }

        bool PassesLeftFilter(ItemBase item)
        {
            return selectedLeftIndex switch
            {
                0 => true,  // 全部
                1 => item.Category == ItemCategory.Consumable,   // 物品（消耗品/药水）
                2 => item.Category == ItemCategory.Equipment,    // 符文
                3 => item.Category == ItemCategory.StoryItem,    // 收藏品（剧情物品）
                _ => true, // 预留按钮，默认全部
            };
        }

        bool PassesTopFilter(ItemBase item)
        {
            if (selectedTopIndex < 0) return true;

            var tag = selectedTopIndex switch
            {
                0 => ShopTag.AP,
                1 => ShopTag.MP,
                2 => ShopTag.HP,
                3 => ShopTag.KO,
                _ => ShopTag.None,
            };

            return (item.shopTag & tag) != 0;
        }

        // ── 选中物品 ──

        void HandleSlotSelected(ShopEntry entry)
        {
            selectedEntry = entry;
            buyCount = 1;
            ShowDetailPanel(entry);
        }

        void ClearSelection()
        {
            selectedEntry = null;
            buyCount = 1;
            if (rightDetailPanel != null) rightDetailPanel.SetActive(false);
        }

        // ── 右侧详情面板 ──

        void ShowDetailPanel(ShopEntry entry)
        {
            if (rightDetailPanel == null || entry?.item == null) return;
            rightDetailPanel.SetActive(true);

            var item = entry.item;

            if (txtName != null) txtName.text = item.Name;
            if (icon != null)
            {
                icon.sprite = item.icon;
                icon.enabled = item.icon != null;
            }
            if (detailCurrencyIcon != null)
            {
                var sprite = item.currencyId == CurrencyId.Spirit ? detailSpiritSprite : detailInkSprite;
                if (sprite != null)
                {
                    detailCurrencyIcon.sprite = sprite;
                    detailCurrencyIcon.enabled = true;
                }
            }
            if (txtIntro != null) txtIntro.text = item.description;
            if (txtHaveNum != null) txtHaveNum.text = "拥有：" + CountOwned(item.id);
            if (txtEffDesc != null) txtEffDesc.text = BuildEffectDesc(item);

            UpdateBuyCountDisplay();
        }

        string BuildEffectDesc(ItemBase item)
        {
            _sb.Clear();

            if (item is Equipment equip)
            {
                if (equip.statMods != null)
                {
                    for (var i = 0; i < equip.statMods.Count; i++)
                    {
                        var mod = equip.statMods[i];
                        if (_sb.Length > 0) _sb.Append('\n');
                        _sb.Append(FormatStatMod(mod));
                    }
                }
                if (equip.extraEffects != null)
                {
                    for (var i = 0; i < equip.extraEffects.Count; i++)
                    {
                        var fx = equip.extraEffects[i];
                        if (fx == null) continue;
                        if (_sb.Length > 0) _sb.Append('\n');
                        _sb.Append(!string.IsNullOrEmpty(fx.displayNameKey) ? fx.displayNameKey : fx.effectTag);
                    }
                }
            }
            else if (item is Consumable cons)
            {
                _sb.Append($"使用场景: {cons.useContext}");
                if (cons.useEffects != null)
                {
                    for (var i = 0; i < cons.useEffects.Count; i++)
                    {
                        var fx = cons.useEffects[i];
                        if (fx == null) continue;
                        _sb.Append('\n');
                        _sb.Append(!string.IsNullOrEmpty(fx.descriptionKey) ? fx.descriptionKey : fx.displayNameKey);
                    }
                }
            }
            else if (item is StoryItem story)
            {
                if (!string.IsNullOrEmpty(story.extraStoryText))
                    _sb.Append(story.extraStoryText);
            }

            return _sb.ToString();
        }

        static string FormatStatMod(StatModifier mod)
        {
            var label = StatDisplayUtil.Label(mod.stat);
            if (Mathf.Abs(mod.flat) > 0.001f && Mathf.Abs(mod.percent) > 0.001f)
                return $"{label} +{mod.flat:0.##} / +{mod.percent * 100f:0.#}%";
            if (Mathf.Abs(mod.flat) > 0.001f)
                return $"{label} +{mod.flat:0.##}";
            if (Mathf.Abs(mod.percent) > 0.001f)
                return $"{label} +{mod.percent * 100f:0.#}%";
            return label;
        }

        // ── 购买数量控制 ──

        void AdjustBuyCount(int delta)
        {
            if (selectedEntry == null) return;
            SetBuyCount(buyCount + delta);
        }

        void SetBuyCount(int value)
        {
            if (selectedEntry == null) return;
            buyCount = Mathf.Clamp(value, 1, CalcMaxBuy());
            UpdateBuyCountDisplay();
        }

        void SetBuyCountMax()
        {
            if (selectedEntry == null) return;
            buyCount = CalcMaxBuy();
            UpdateBuyCountDisplay();
        }

        int CalcMaxBuy()
        {
            if (selectedEntry?.item == null || shopService == null) return 1;

            var item = selectedEntry.item;
            var price = shopService.GetPrice(selectedEntry);
            if (price <= 0) return 1;

            var stock = shopService.GetRemainingStock(item.id);
            var maxByStock = stock < 0 ? int.MaxValue : stock;

            var currency = item.currencyId;
            var currencyAmount = wallet != null ? wallet.Get(currency) : 0;
            var maxByPrice = currencyAmount / price;

            var owned = CountOwned(item.id);
            var maxByStack = item.MaxStack - owned;

            return Mathf.Max(1, Mathf.Min(maxByStock, maxByPrice, maxByStack));
        }

        void UpdateBuyCountDisplay()
        {
            if (txtBuyNum != null)
                txtBuyNum.text = buyCount.ToString();

            // 更新购买按钮上的总价
            if (txtBuyPrice != null && selectedEntry != null && shopService != null)
            {
                var unitPrice = shopService.GetPrice(selectedEntry);
                var totalPrice = unitPrice * buyCount;
                var currencyName = GetCurrencyName(selectedEntry.item.currencyId);
                txtBuyPrice.text = totalPrice + " " + currencyName + " 购买";
            }
        }

        static string GetCurrencyName(CurrencyId id)
        {
            return id switch
            {
                CurrencyId.Ink => "墨",
                CurrencyId.Spirit => "灵",
                _ => id.ToString(),
            };
        }

        // ── 购买 ──

        void OnClickBuy()
        {
            if (selectedEntry?.item == null || shopService == null) return;

            var itemId = selectedEntry.item.id;
            var result = shopService.TryBuy(itemId, buyCount);
            if (result == BuyResult.Success)
            {
                buyCount = 1;
                Refresh(itemId);
            }
        }

        // ── 货币 ──

        void RefreshInk()
        {
            if (wallet == null) return;
            if (txtInk != null) txtInk.text = wallet.Get(CurrencyId.Ink).ToString();
            if (txtSpirit != null) txtSpirit.text = wallet.Get(CurrencyId.Spirit).ToString();
        }

        void HandleWalletChanged(CurrencyId _, int __)
        {
            RefreshInk();
            if (selectedEntry != null) ShowDetailPanel(selectedEntry);
        }

        // ── 工具 ──

        void EnsureShopOpen()
        {
            if (shopService != null && !shopService.IsOpen)
                shopService.Open();
        }

        void EnsureScrollLayout()
        {
            if (content == null) return;

            // 确保 Content 有 ContentSizeFitter，让高度随子项自动扩展，ScrollView 才能滚动
            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        int CountOwned(string definitionId)
        {
            if (inventory == null || string.IsNullOrEmpty(definitionId)) return 0;
            var total = 0;
            var items = inventory.Items;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].definitionId == definitionId)
                    total += items[i].count;
            }
            return total;
        }
    }
}
