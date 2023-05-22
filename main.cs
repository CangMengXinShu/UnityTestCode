public static class ABTool
{
    public static AssetType loadUniversalABAsset<AssetType>(string assetName) where AssetType : Object
    {
        return loadSceneABAsset<AssetType>("Universal", assetName);
    }
    
    public static AssetType loadSceneABAsset<AssetType>(string sceneName, string assetName) where AssetType : Object
    {
        var type = typeof(AssetType);
        if (type == typeof(Material))
        {
            return loadABAsset<AssetType>(sceneName + "/mat", assetName);
        }
        else if (type == typeof(GameObject))
        {
            return loadABAsset<AssetType>(sceneName + "/prefab", assetName);
        }
        else if (type == typeof(Shader))
        {
            return loadABAsset<AssetType>(sceneName + "/shader", assetName);
        }
        return null;
    }
    
    public static AssetType loadABAsset<AssetType>(string abName, string assetName) where AssetType : Object
    {
        string streamingAssetsBundlePath = Path.Combine(Application.streamingAssetsPath,
            Path.GetDirectoryName(abName),
            Path.GetDirectoryName(abName));
        AssetBundle streamingAssetsBundle = AssetBundle.LoadFromFile(streamingAssetsBundlePath);
        AssetBundleManifest assetBundleManifest = streamingAssetsBundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");
        foreach (var item in assetBundleManifest.GetAllDependencies(Path.GetFileName(abName)))
        {
            AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath + "/" + Path.GetDirectoryName(abName) + "/" + item));
        }
        AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, abName));
        return assetBundle.LoadAsset<AssetType>(assetName);
    }
}


public class InputM : MonoBehaviour
{
    public static InputM Instance;
    
    public GameObject selectRectPrefab;
    private GameObject _selectRect;

    public List<CharacterController> _selectedCharacters = new List<CharacterController>();

    private Vector3 _dragStartPos;
    private Vector3 _dragEndPos;
    private Vector3 dragCenterPos;
    private Vector3 dragSize;
    private bool _isDraging = false;
    private bool _isSingSelect = false;
    
    private void Awake()
    {
        Instance = this;
        
        selectRectPrefab = ABTool.loadUniversalABAsset<GameObject>("selectRect.prefab");
        if (!_selectRect)
        {
            _selectRect = Instantiate(selectRectPrefab);
            _selectRect.SetActive(false);
        }
    }

    void Update()
    {
        SelectUpdate();
    }

    void SelectUpdate()
    {
        // 框选
        CheckSelect();
        // 如果没有框选 也就是单选，并且鼠标松开了
        if (_isSingSelect && Input.GetMouseButtonUp(0))
        {
            // Debug.Log("单选");
            CheckSingleSelect();
        }
    }

    private void CheckSingleSelect()
    {
        _selectedCharacters.Clear();
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000, LayerMask.GetMask("Chosen")))
        {
            if (hitInfo.collider.GetComponent<CharacterController>())
            {
                // 很有可能就是最终结束的位置
                _selectedCharacters.Add(hitInfo.collider.GetComponent<CharacterController>());
                // 选中逻辑 hitinfos[i].collider.GetComponent<CharacterController>()
            }
        }
    }

    // 检查拖拽选择
    private void CheckSelect()
    {
        // 开始拖拽
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // 只考虑地面
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                // 得到拖拽的起始点坐标
                _dragStartPos = hitInfo.point;
            }
        }
        // 拖拽过程中
        else if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                // 很有可能就是最终结束的位置
                _dragEndPos = hitInfo.point;
            }
            if (Vector3.Distance(_dragStartPos, _dragEndPos) > 0.2f)
            {
                _isDraging = true;
                // 显示绿框
                _selectRect.SetActive(true);
                // 获取中心点的位置
                dragCenterPos = (_dragStartPos + _dragEndPos) / 2;
                // 计算缩放
                dragSize = _dragEndPos - _dragStartPos; // 不是为了计算方向

                _selectRect.transform.position = dragCenterPos + new Vector3(0, 0.2f, 0);
                // transform.localScale = new Vector3(dragSize.x, 1, dragSize.z);
                _selectRect.transform.localScale = new Vector3(dragSize.x, dragSize.z, 1);
            }
        }
        // 结束拖拽
        else if (Input.GetMouseButtonUp(0))
        {
            if (_isDraging)
            {
                _isDraging = false;
                _selectRect.SetActive(false);
                CheckSelectCharacters();
                _isSingSelect = false;
            }
            // 没有拖拽，相当于原地点击
            else
            {
                _isSingSelect = true;
            }
    
        }
    }

    private void CheckSelectCharacters()
    {
        // 之前挨个取消选中
        _selectedCharacters.Clear();
        dragSize = new Vector3(Mathf.Abs(dragSize.x / 2), 10, Mathf.Abs(dragSize.z / 2));
        // 做物理碰撞检测 获取范围内的所有单位
        RaycastHit[] hitinfos = Physics.BoxCastAll(dragCenterPos, dragSize, Vector3.up, Quaternion.identity, 1000, LayerMask.GetMask("Chosen"));
        // Debug.Log(LayerMask.NameToLayer("Chosen"));
        // 只让第一个人说话
        // if (hitinfos.Length > 0)
        // {
        //     hitinfos[0].collider.GetComponent<PlayerUnit>().SelectAudio();
        // }
        // 选中每一个人
        for (int i = 0; i < hitinfos.Length; i++)
        {
            // Debug.Log(hitinfos[i].collider.gameObject);
            if (hitinfos[i].collider.GetComponent<CharacterController>())
            {
                _selectedCharacters.Add(hitinfos[i].collider.GetComponent<CharacterController>());
                // 选中逻辑 hitinfos[i].collider.GetComponent<CharacterController>()
            }
        }
    }
    
}

public class CharacterController : MonoBehaviour
{
    public IdleState IdleState { get; }
    public MovingState MovingState { get; }
    // ...
    
    [SerializeField] private ActionManager actionManager;
    [SerializeField] private Animator animator;
    CharacterData characterData;
    
    private State currentState;

    public void ReceiveCommand(Command command)
    {
        switch (command)
        {
            case Command.Move:
                ChangeState(MovingState);
                break; 
        }
    }
    
    public void ReceiveCommand(Skill skill) 
    {
        actionManager.Play(skill);
    }
    
    void ChangeState(State newState)
    {
        currentState.Exit();
        currentState = newState;
        newState.Enter(this);
    }
    
    public void OnActionStart(Action action)
    {
        // ...
    }

    public void OnActionEnd(Action action)
    {
        // ...
    }

}

public abstract class State  
{
    public abstract void Enter(CharacterController characterController);
    public abstract void Update(); 
    
    public void Exit() {}
}

public class ActionManager  
{
    Dictionary<string, Action> actions = new Dictionary<string, Action>();
    
    public void Play(string actionID)
    {
        Action action = Actions[actionID];
        StartCoroutine(ExecuteAction(action));
    }
    
    IEnumerator ExecuteAction(Action action)
    { 
        characterController.OnActionStart(action);
        CurrentAction = action;
    
        while (CurrentAction != null) 
        {
            CurrentAction.Perform();
            Character.Animation.Play(CurrentAction.AnimationClip);  

            // 检查中断和Duration
            if (CurrentAction.IsInterrupted || CurrentAction.Duration <= 0)
            {
                CurrentAction.Cleanup();
                CurrentAction = null;
            }
            yield return null;
        }
        characterController.OnActionEnd(CurrentAction);
    }
    
    public void Play(Skill skill)
    {
        actions[skill.ID].Perform();
    }
    
    public void OnActionFrame(string actionID)
    {
        // ...
    }  
    public void OnActionFrame(string actionID)
    {
        switch (actionID)
        {
            case "Skill1":
                // 调用Skill1类内方法
                Skill1.UpdateDetails();  
                break;
            case "Attack1":
                // 调用Attack1类内方法
                Attack1.UpdateDetails(); 
                break;
        }
    } 
}

public class Action  
{
    protected CharacterController characterController;
    [SerializeField]
    float Duration;

    bool IsInterrupted;
    bool CanInterrupt = true;

    public Action(CharacterController characterController)
    {
        this.characterController = characterController;
    }

    void Cleanup()
    {
        // 判断是否消耗资源和CD
    }

    void Perform()
    {
        // 驱动技能效果和状态改变  
        // 更新IsInterrupted状态
    }
    
}


// Attribute类:包含基础值、计算值和修饰词字典
public class Attribute
{
    public string Name;
    public float BaseValue;
    public float CalculatedValue;
    public Dictionary<string, PriorityQueue<AttributeModifier>> FixedModifiers; 
    public Dictionary<string, PriorityQueue<AttributeModifier>> PercentModifiers;
}

// AttributeModifier类:表示单个修饰词  
public class AttributeModifier : IComparable
{
    public Effect SourceEffect;        // 来源Effect
    public string tag;
    public Attribute Attribute;         // 作用Attribute
    public float Value;                // 数值  
    public bool Percent;              // 是否百分比
    public bool Dirty;                // 是否需要重新计算

    public int CompareTo(object obj)
    {
        // 用于PriorityQueue中的比较,数值更大者排前
    } 
}

// Effect类:表示效果(装备或Buff),包含多个Modifier
public abstract class Effect 
{
    public List<AttributeModifier> Modifiers;
}

public class Equipment : Effect{}      // 装备类 
// OccupationEffect类:表示职业加成效果  
public class OccupationEffect : Effect 
{}
// Buff类   
public class Buff : Effect {
    public float Duration { get; set; }
    
    // Update方法在每帧调用
    void Update()
    {
        Duration -= Time.deltaTime;
        if (Duration <= 0)
        {
            // 发出Buff结束事件
            OnBuffEnded?.Invoke(this); 
        }
    }
}          
// 用于存储AttributeModifier,方便选取数值最大者
public class PriorityQueue<T> where T : IComparable
{
 // 标准优先队列实现  
}

public class CharacterData
{
    // 基本属性
    public Dictionary<string, float> BaseAttributes { get; private set; }
    
    // 装备列表
    public List<Equipment> Equipments { get; private set; }
    
    // Buff列表
    public List<Buff> Buffs { get; private set; }

    public List<OccupationEffect> OccupationEffects;
    
    // 角色最终计算出的属性结果
    public Dictionary<string, float> FinalAttributes { get; set; }

    // 职业等级字典,键为(string, int),值为OccupationLevelData
    public static Dictionary<(string, int), OccupationLevelData> occupationLevelData;
    
    public CharacterData()
    {
        ImportFromConfig(this);
    }

    public void GainOccupationEXP(Occupation occupation, int exp)
    {
        (string name, int level) = (occupation.Name, level + 1); 
        OccupationLevelData levelData = occupationLevelData[(name, level)]; 
        RemoveEffect(currentEffect);
       
        OccupationEffects[(name, level)] = levelData.BaseEffect;
        Skills[(name, level)] = levelData.Skills; 
    }

    // 事件
    public delegate void EffectAddedDelegate(Effect effect);
    public event EffectAddedDelegate OnEffectAdded;

    public delegate void EffectRemovedDelegate(Effect effect);
    public event EffectRemovedDelegate OnEffectRemoved;
    
    public void AddEffectModifiers(Effect effect)
    {
        // 从effect中获取所有Modifier
        var modifiers = effect.Modifiers;
        
        // 将Modifier添加到相应Attribute的Modifier字典中
        foreach (var modifier in modifiers)
        {
            var attribute = characterData.Attributes[modifier.Attribute.Name];
            if (modifier.Percent)
                attribute.PercentModifiers[effect.Source].Enqueue(modifier);
            else 
                attribute.FixedModifiers[effect.Source].Enqueue(modifier);
        }
        
        // 标记Attribute为Dirty,下次属性计算时会重新计算
        foreach (var modifier in modifiers)
        {
            characterData.Attributes[modifier.Attribute.Name].Dirty = true;
        }
    }

    public void RemoveEffectModifiers(Effect effect)
    {
        // 从相应Attribute的Modifier字典中移除Modifier
        foreach (var modifier in effect.Modifiers)
        {
            var attribute = characterData.Attributes[modifier.Attribute.Name];
            if (modifier.Percent)
                attribute.PercentModifiers[effect.Source].Remove(modifier);
            else
                attribute.FixedModifiers[effect.Source].Remove(modifier);
        }
        
        // 标记Attribute为Dirty,下次属性计算时会重新计算
        foreach (var modifier in effect.Modifiers)
        {
            characterData.Attributes[modifier.Attribute.Name].Dirty = true;
        }
    }

    public void AddEffect(Effect effect)
    {
        if (effect is Buff) Buffs.Add((Buff)effect);
        else if (effect is OccupationEffect) OccupationEffects.Add((OccupationEffect)effect);  
        
        EffectAddedDelegate += RemoveBuff;
        AddEffectModifiers(effect);
        OnEffectAdded?.Invoke(effect); 
    }

    public void RemoveEffect(Effect effect)
    {
        if (effect is Buff) Buffs.Remove((Buff)effect);
        else if (effect is OccupationEffect) OccupationEffects.Remove((OccupationEffect)effect);
        
        RemoveEffectModifiers(effect);
        OnEffectRemoved?.Invoke(effect);
        BuffEndedDelegate -= RemoveBuff;
    }
}

public static class AttributeSystem
{
    
    // 从配置表导入所有数据,构建Attribute和Effect实例
    public void ImportFromConfig(CharacterData characterData)
    {
        // 读取所有Attribute配置,构建Attribute实例
        characterData.BaseAttributes = ReadAttributeConfigs();

        // 读取所有Effect(装备&Buff)配置,构建Effect实例
        characterData.Equipments = ReadBuffsConfigs();
        // ...
    }
    public static void CalculateFinalAttributes(CharacterData character)
    {
        // 先计算固定加成
        foreach (var attribute in character.Attributes.Values)
        {
            // 跳过不需要更新的Attribute
            if (!attribute.Dirty) continue; 
            
            // 特殊处理tag为True的修饰词,所有修饰词叠加
            if (attribute.Tag == "其他")
            {
                attribute.CalculatedValue = attribute.BaseValue;
                foreach (var modifiers in attribute.FixedModifiers)
                {
                    attribute.CalculatedValue += modifiers.Value.Sum(m => m.Value);
                }
            }
            else  // 普通词条只选择最大修饰词
            {
                var maxModifier = attribute.FixedModifiers.GetMax(); 
                attribute.CalculatedValue = attribute.BaseValue + maxModifier.Value; 
            }
            
            attribute.Dirty = false;
        }

        // 再计算百分比加成
        foreach (var attribute in character.Attributes.Values)
        {
            // 跳过不需要更新的Attribute
            if (!attribute.Dirty) continue;
            
            // 特殊处理tag为True的修饰词,所有修饰词叠加
            if (attribute.Tag == "其他")
            {
                foreach (var modifiers in attribute.PercentModifiers)
                {
                    var sumModifier = modifiers.Value.Sum(m => m.Value);
                    attribute.CalculatedValue *= 1 + sumModifier / 100; 
                }
            }
            else   // 普通词条只选择最大修饰词
            {
                var maxModifier = attribute.PercentModifiers.GetMax();
                attribute.CalculatedValue *= 1 + maxModifier.Value / 100; 
            }
            
            attribute.Dirty = false;
        }
    }
}





