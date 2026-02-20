// Assets/sandbox/boat/BoatManager.cs
// 軸選択（X/Y/Z）版：startX→goalX 等の一軸進行。幅(lateral)・高さ(vertical)は残り2軸で制約。
// 要件：初回散開／Start付近は“沸かせない”／前詰まり時は迂回／境界寄せ／境界外は即リスポーン／Goalでループ。
// UdonSharp想定：ジェネリクス/ LINQ/ ネスト型 未使用。
// 修正：SetLayerRecursive(階層全体layer変更)をやめ、Colliderが付くGameObjectだけ boatLayer にする（描画Layerを壊さない）

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public enum Axis { X, Y, Z }

public class BoatManager : UdonSharpBehaviour
{
    [Header("■ プール（各要素はユニーク & Collider 必須 / Trigger=OFF）")]
    public GameObject[] pool;

    [Header("■ 軸選択 & Start/Goal（ワールド座標のスカラー）")]
    public Axis driveAxis = Axis.Z;
    public float startX = 0f,  goalX = 100f;
    public float startY = 0f,  goalY = 0f;
    public float startZ = 0f,  goalZ = 100f;

    [Header("■ 幅・高さレンジ（ワールド）")]
    [Tooltip("横方向の幅（進行軸に応じて自動で X/Y/Z のどれかに割当）")]
    public Vector2 lateralRange  = new Vector2(-6f, 6f);
    [Tooltip("縦方向の幅（進行軸に応じて自動で X/Y/Z のどれかに割当）")]
    public Vector2 verticalRange = new Vector2(-0.3f, 0.3f);

    [Header("■ 速度")]
    public float speedMin = 2f;
    public float speedMax = 5f;
    public float minFollowSpeed = 0.8f;
    public float accel = 2f;
    public float brake = 4f;

    [Header("■ 追従・迂回")]
    public float lookAhead = 6f;         // 前方レイ距離
    public float swerveSpeed = 2f;       // 横移動速度（lateral）
    public float noseOffset = 1f;        // 船首オフセット
    public bool  allowLateralMovement = true; // 迂回許可
    public float sideFOV = 0.7071f;      // 左右レイの斜め係数（√2/2）

    [Header("■ スポーン（Start付近は“沸かせない”）")]
    public bool activateAllAtStart = true;
    public bool initialScatter = true;      // 初回：全域ランダム散開
    public float minSpawnSeparation = 3f;   // 初回 横-前 最小距離
    public int   initialScatterTries = 16;
    public bool  autoSpawn = false;
    public float spawnInterval = 1f;
    public int   maxActive = 0;             // 0=無制限
    public float spawnGapMin = 6.0f;        // Startからこの前方距離以内に居たら沸かせない
    public float lateralClear = 1.6f;       // 同レーンとみなす横許容差

    [Header("■ 境界制御")]
    public bool  hardClamp = true;           // 毎フレーム範囲内クランプ（保険）
    public bool  respawnIfOut = true;        // 出た瞬間リスポーン
    public float failSafeMargin = 0.05f;

    [Header("■ エッジ・ホールド（境界に近づいたら内側へ寄せる）")]
    public bool  edgeHold = true;
    public float edgeHoldMargin    = 0.25f;
    public float edgeReleaseMargin = 0.40f;
    public float edgeHoldMinSpeed  = 0.0f;
    public float edgeInwardNudge   = 1.0f;

    [Header("■ 物理/レイヤ")]
    public int   boatLayer = 10;             // 船だけのLayer（Raycast対象）
    public bool  forceKinematic = true;
    public bool  freezeRotation = true;
    public bool  warnNonUniformScale = true;
    public bool  normalizeLocalScale = false;

    [Header("■ パフォーマンス")]
    public int updateBatchSize = 25;

    [Header("■ Gizmos 表示")]
    public bool showGizmos = true;              // 枠の表示ON/OFF
    public bool onlyWhenSelected = false;       // 選択時のみ表示
    public Color gizmoBoxColor  = new Color(0f, 1f, 1f, 1f);        // 枠線（シアン）
    public Color gizmoLineColor = new Color(1f, 0.92f, 0.016f, 1f); // 中央ライン（黄）

    // ---- 内部状態 ----
    private float[] _cur, _tgt, _bias;
    private bool[]  _active;
    private bool[]  _edgeHolding;
    private float[] _fixedLat; // 横移動OFF時の固定 lateral
    private int _activeCount;
    private float _nextSpawn;
    private int _mask;
    private int _updateOffset;

    // 軸ごとの昇順レンジと進行符号
    private float _pLo, _pHi;   // 進行軸の範囲（昇順）
    private int   _sign;        // +1 or -1（goal - start の符号）

    void Start()
    {
        int n = (pool != null) ? pool.Length : 0;
        _cur         = new float[n];
        _tgt         = new float[n];
        _bias        = new float[n];
        _active      = new bool[n];
        _edgeHolding = new bool[n];
        _fixedLat    = new float[n];

        NormalizeAll();
        ResolveAxisRanges();

        _mask = 1 << boatLayer;
        _activeCount = 0;
        _nextSpawn = Time.time + spawnInterval;
        _updateOffset = 0;

        // 初期OFF + レイヤ/物理整備
        for (int i = 0; i < n; i++)
        {
            GameObject go = pool[i];
            if (!go) continue;

            // 修正：描画Layerを壊さない。Colliderが付いているGOのみLayer変更
            SetLayerForCollidersOnly(go.transform, boatLayer);

            Harden(go);
            go.SetActive(false);
        }

        // 既にONは取り込み
        AdoptAlreadyActive();

        // 初回ON（ランダム散開）
        if (activateAllAtStart && pool != null)
            for (int i = 0; i < pool.Length; i++) Activate(i, initialScatter);

        ValidatePool();
    }

    void Update()
    {
        if (pool == null) return; // NRE防止
        float dt = Time.deltaTime; if (dt > 0.05f) dt = 0.05f; // 低FPS時の暴れ抑制

        // 自動スポーン（上限未満）
        if (autoSpawn && (maxActive <= 0 || _activeCount < maxActive))
            if (Time.time >= _nextSpawn) { SpawnOne(); _nextSpawn = Time.time + spawnInterval; }

        int n = pool.Length;
        int s = 0, e = n;
        if (updateBatchSize > 0)
        {
            s = _updateOffset;
            e = Mathf.Min(s + updateBatchSize, n);
            _updateOffset = (e >= n) ? 0 : e;
        }

        Vector3 fwd = ForwardVector();
        Vector3 side = LateralUnit();

        for (int i = s; i < e; i++)
        {
            if (!_active[i]) continue;
            GameObject go = pool[i];
            if (!go || !go.activeSelf)
            {
                if (_active[i]) { _active[i] = false; _activeCount = (_activeCount > 0) ? _activeCount - 1 : 0; }
                continue;
            }

            // ワールド→“コリドー局所”（lat, vert, prog）
            Vector3 l = WorldToLocal3(go.transform.position);

            // 出た瞬間リスポーン
            if (respawnIfOut && IsOutsideLocal(l, 0f))
            { RespawnAtStart(ref go, i); continue; }

            // 前方詰まり：レイで確認（前・左右は fwd ± side の斜めで見る）
            Vector3 origin = go.transform.position + fwd * noseOffset;
            bool frontBlocked = RayHitOther(origin, fwd, lookAhead, go);
            bool leftBlocked  = RayHitOther(origin, (fwd + side).normalized, lookAhead * sideFOV, go);
            bool rightBlocked = RayHitOther(origin, (fwd - side).normalized, lookAhead * sideFOV, go);

            // 速度
            if (frontBlocked) _cur[i] = Mathf.Max(minFollowSpeed, _cur[i] - brake * dt);
            else              _cur[i] = Mathf.MoveTowards(_cur[i], _tgt[i], accel * dt);

            // 迂回（横移動）
            if (allowLateralMovement && frontBlocked)
            {
                float sgn = 0f;
                if (leftBlocked && !rightBlocked) sgn = -1f;      // 右へよける
                else if (!leftBlocked && rightBlocked) sgn = +1f; // 左へよける
                else if (!leftBlocked && !rightBlocked) sgn = (_bias[i] >= 0f) ? +1f : -1f;

                l.x += sgn * swerveSpeed * dt;

                // 端に触れたら内向きバイアス
                float before = l.x;
                l.x = Mathf.Clamp(l.x, lateralRange.x, lateralRange.y);
                if (Mathf.Abs(l.x - before) > 1e-6f)
                {
                    if (l.x <= lateralRange.x + 0.05f) _bias[i] = +1f;
                    else if (l.x >= lateralRange.y - 0.05f) _bias[i] = -1f;
                }
            }

            // 境界に近づけば内側へ寄せる
            if (edgeHold)
            {
                bool nearEdge =
                    (l.x <= lateralRange.x + edgeHoldMargin) ||
                    (l.x >= lateralRange.y - edgeHoldMargin) ||
                    (l.y <= verticalRange.x + edgeHoldMargin) ||
                    (l.y >= verticalRange.y - edgeHoldMargin) ||
                    ((_sign > 0 && l.z >= _pHi - edgeHoldMargin) ||
                     (_sign < 0 && l.z <= _pLo + edgeHoldMargin));

                if (nearEdge) _edgeHolding[i] = true;

                if (_edgeHolding[i])
                {
                    _cur[i] = Mathf.MoveTowards(_cur[i], edgeHoldMinSpeed, brake * dt);

                    // 横移動ON/OFFに関係なく微押ししたい場合は下の if を外す
                    if (allowLateralMovement)
                    {
                        float targetX = Mathf.Clamp(l.x,
                            lateralRange.x + edgeReleaseMargin,
                            lateralRange.y - edgeReleaseMargin);
                        l.x = Mathf.MoveTowards(l.x, targetX, edgeInwardNudge * dt);
                    }

                    bool safe =
                        (l.x > lateralRange.x + edgeReleaseMargin) &&
                        (l.x < lateralRange.y - edgeReleaseMargin) &&
                        (l.y > verticalRange.x + edgeReleaseMargin) &&
                        (l.y < verticalRange.y - edgeReleaseMargin) &&
                        ((_sign > 0 && l.z < _pHi - edgeReleaseMargin) ||
                         (_sign < 0 && l.z > _pLo + edgeReleaseMargin));

                    if (safe) _edgeHolding[i] = false;
                }
            }

            // 前進（進行軸）
            l.z += _sign * _cur[i] * dt;

            // 最終クランプ（保険）
            if (hardClamp)
            {
                l.x = Mathf.Clamp(l.x, lateralRange.x, lateralRange.y);
                l.y = Mathf.Clamp(l.y, verticalRange.x, verticalRange.y);
                l.z = Mathf.Clamp(l.z, _pLo, _pHi);
            }
            if (!allowLateralMovement) l.x = Mathf.Clamp(_fixedLat[i], lateralRange.x, lateralRange.y);

            // GoalでStartに戻す
            bool needLoop = (_sign > 0) ? (l.z >= _pHi) : (l.z <= _pLo);
            if (needLoop)
            {
                l.z = (_sign > 0) ? _pLo : _pHi;
                l.x = Random.Range(lateralRange.x, lateralRange.y);
                l.y = Mathf.Clamp((verticalRange.x + verticalRange.y) * 0.5f, verticalRange.x, verticalRange.y);

                float cruise = Random.Range(speedMin, speedMax);
                _tgt[i] = cruise;
                _cur[i] = Mathf.Min(_cur[i], _tgt[i]);
                _bias[i] = (Random.value < 0.5f) ? -1f : 1f;

                if (!allowLateralMovement) _fixedLat[i] = l.x;
            }

            // 反映
            go.transform.position = Local3ToWorld(l);
        }
    }

    // ==== 軸マッピング：World<->Local3(lat,vert,prog) ====
    private Vector3 WorldToLocal3(Vector3 p)
    {
        Vector3 l;
        if (driveAxis == Axis.X)
        {
            l.x = p.z;   // lateral = Z
            l.y = p.y;   // vertical = Y
            l.z = p.x;   // progress = X
        }
        else if (driveAxis == Axis.Y)
        {
            l.x = p.x;   // lateral = X
            l.y = p.z;   // vertical = Z
            l.z = p.y;   // progress = Y
        }
        else
        {
            l.x = p.x;   // lateral = X
            l.y = p.y;   // vertical = Y
            l.z = p.z;   // progress = Z
        }
        return l;
    }

    private Vector3 Local3ToWorld(Vector3 l)
    {
        Vector3 p;
        if (driveAxis == Axis.X)
        {
            p.x = l.z;  p.y = l.y;  p.z = l.x;
        }
        else if (driveAxis == Axis.Y)
        {
            p.x = l.x;  p.y = l.z;  p.z = l.y;
        }
        else
        {
            p.x = l.x;  p.y = l.y;  p.z = l.z;
        }
        return p;
    }

    private Vector3 ForwardVector()
    {
        if (driveAxis == Axis.X) return (_sign > 0) ? Vector3.right : Vector3.left;
        if (driveAxis == Axis.Y) return (_sign > 0) ? Vector3.up    : Vector3.down;
        return (_sign > 0) ? Vector3.forward : Vector3.back;
    }

    private Vector3 LateralUnit()
    {
        if (driveAxis == Axis.X) return Vector3.forward; // lateral=Z
        if (driveAxis == Axis.Y) return Vector3.right;   // lateral=X
        return Vector3.right;                            // lateral=X
    }

    // ==== スポーン/採用 ====
    public void SpawnOne()
    {
        if (pool == null) return;
        int idx = FindInactiveIndex(); if (idx < 0) return;
        Activate(idx, false);
    }

    private int FindInactiveIndex()
    {
        if (pool == null) return -1;
        for (int i = 0; i < pool.Length; i++) if (!_active[i]) return i;
        return -1;
    }

    private void Activate(int i, bool scatter)
    {
        if (pool == null) return;
        GameObject go = pool[i]; if (!go) return;
        Harden(go);

        Vector3 l;
        if (scatter)
        {
            l = FindSeparatedSpawnLocal();
        }
        else
        {
            // Start端：近傍に先行がいれば「沸かせない」
            bool ok = false; float candLat = 0f;
            for (int t = 0; t < 6; t++)
            {
                float tryLat = Random.Range(lateralRange.x, lateralRange.y);
                if (CanSpawnAtStartLat(tryLat)) { candLat = tryLat; ok = true; break; }
            }
            if (!ok) return;

            l.x = candLat;
            l.y = Random.Range(verticalRange.x, verticalRange.y);
            l.z = (_sign > 0) ? _pLo : _pHi; // Start側
        }

        // クランプ
        l.x = Mathf.Clamp(l.x, lateralRange.x, lateralRange.y);
        l.y = Mathf.Clamp(l.y, verticalRange.x, verticalRange.y);
        l.z = Mathf.Clamp(l.z, _pLo, _pHi);

        go.transform.position = Local3ToWorld(l);

        if (go.GetComponent<Collider>() == null)
            Debug.LogWarning("[BoatManager] " + go.name + " に Collider がありません（TriggerはOFFで付けろ）");

        // 修正：描画Layerを壊さない。Colliderが付いているGOのみLayer変更
        SetLayerForCollidersOnly(go.transform, boatLayer);

        go.SetActive(true);

        float cruise = Random.Range(speedMin, speedMax);
        _tgt[i]  = cruise;
        _cur[i]  = cruise * 0.7f;
        _bias[i] = (Random.value < 0.5f) ? -1f : 1f;

        _active[i] = true;
        _activeCount++;
        _edgeHolding[i] = false;
        if (!allowLateralMovement) _fixedLat[i] = l.x;
    }

    private bool CanSpawnAtStartLat(float candLat)
    {
        if (pool == null) return false;
        float z0 = (_sign > 0) ? _pLo : (_pHi - spawnGapMin);
        float z1 = (_sign > 0) ? (_pLo + spawnGapMin) : _pHi;

        for (int k = 0; k < pool.Length; k++)
        {
            if (!_active[k]) continue;
            GameObject other = pool[k];
            if (!other || !other.activeSelf) continue;

            Vector3 ol = WorldToLocal3(other.transform.position);
            if (Mathf.Abs(ol.x - candLat) > lateralClear) continue;
            if (ol.z >= Mathf.Min(z0, z1) && ol.z <= Mathf.Max(z0, z1)) return false;
        }
        return true;
    }

    private void RespawnAtStart(ref GameObject go, int i)
    {
        Vector3 l;
        l.x = Random.Range(lateralRange.x, lateralRange.y);
        l.y = Random.Range(verticalRange.x, verticalRange.y);
        l.z = (_sign > 0) ? _pLo : _pHi;

        go.transform.position = Local3ToWorld(l);

        float cruise = Random.Range(speedMin, speedMax);
        _tgt[i]  = cruise;
        _cur[i]  = Mathf.Min(_cur[i], _tgt[i]);
        _bias[i] = (Random.value < 0.5f) ? -1f : 1f;

        _edgeHolding[i] = false;
        if (!allowLateralMovement) _fixedLat[i] = l.x;
    }

    // 初回：横-前で最小距離確保（高さは中央）
    private Vector3 FindSeparatedSpawnLocal()
    {
        float minSqr = minSpawnSeparation * minSpawnSeparation;

        for (int t = 0; t < initialScatterTries; t++)
        {
            float lx = Random.Range(lateralRange.x, lateralRange.y);
            float lz = Random.Range(_pLo, _pHi);
            bool ok = true;

            for (int k = 0; k < pool.Length; k++)
            {
                if (!_active[k]) continue;
                GameObject other = pool[k];
                if (!other || !other.activeSelf) continue;

                Vector3 ol = WorldToLocal3(other.transform.position);
                float dx = ol.x - lx;
                float dz = ol.z - lz;
                if (dx * dx + dz * dz < minSqr) { ok = false; break; }
            }
            if (ok)
            {
                float ly = 0.5f * (verticalRange.x + verticalRange.y);
                Vector3 posL = new Vector3(lx, ly, lz);
                posL.x = Mathf.Clamp(posL.x, lateralRange.x, lateralRange.y);
                posL.y = Mathf.Clamp(posL.y, verticalRange.x, verticalRange.y);
                posL.z = Mathf.Clamp(posL.z, _pLo, _pHi);
                return posL;
            }
        }

        // 妥協
        Vector3 fb;
        fb.x = Random.Range(lateralRange.x, lateralRange.y);
        fb.y = 0.5f * (verticalRange.x + verticalRange.y);
        fb.z = Random.Range(_pLo, _pHi);
        fb.x = Mathf.Clamp(fb.x, lateralRange.x, lateralRange.y);
        fb.y = Mathf.Clamp(fb.y, verticalRange.x, verticalRange.y);
        fb.z = Mathf.Clamp(fb.z, _pLo, _pHi);
        return fb;
    }

    // ==== 判定/整備 ====
    private bool IsOutsideLocal(Vector3 l, float margin)
    {
        if (l.x < lateralRange.x - margin || l.x > lateralRange.y + margin) return true;
        if (l.y < verticalRange.x - margin || l.y > verticalRange.y + margin) return true;
        if (l.z < _pLo - margin || l.z > _pHi + margin) return true;
        return false;
    }

    private void Harden(GameObject go)
    {
        if (forceKinematic)
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                if (freezeRotation) rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        if (warnNonUniformScale)
        {
            Vector3 ls = go.transform.lossyScale;
            if (Mathf.Abs(ls.x - ls.y) > 1e-4f || Mathf.Abs(ls.x - ls.z) > 1e-4f || Mathf.Abs(ls.y - ls.z) > 1e-4f)
                Debug.LogWarning("[BoatManager] 非等方スケール検出: " + go.name + " lossyScale=" + go.transform.lossyScale + "（歪みの主因。親も含め 1,1,1 推奨）");
        }
        if (normalizeLocalScale) go.transform.localScale = Vector3.one;
    }

    // 修正の本体：Colliderが付いているGameObjectだけ layer を変更する（RendererだけのGOは触らない）
    private void SetLayerForCollidersOnly(Transform t, int layer)
    {
        if (t == null) return;

        bool has3D = t.GetComponent<Collider>() != null;
        bool has2D = t.GetComponent<Collider2D>() != null;

        if (has3D || has2D)
            t.gameObject.layer = layer;

        int childCount = t.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform c = t.GetChild(i);
            if (c != null) SetLayerForCollidersOnly(c, layer);
        }
    }

    // 旧：階層全体を変更（現在は未使用。残しても動作に影響しない）
    private void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        int childCount = t.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform c = t.GetChild(i);
            if (c != null) SetLayerRecursive(c, layer);
        }
    }

    // 自分階層は除外（root比較）
    private bool RayHitOther(Vector3 origin, Vector3 dir, float dist, GameObject self)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, dist, _mask, QueryTriggerInteraction.Ignore))
        {
            Transform rtSelf = self != null ? self.transform.root : null;
            Transform rtHit = (hit.collider != null) ? hit.collider.transform.root : null;
            if (rtSelf != null && rtHit != null && rtHit != rtSelf) return true;
        }
        return false;
    }

    private void AdoptAlreadyActive()
    {
        if (pool == null) return;
        for (int i = 0; i < pool.Length; i++)
        {
            GameObject go = pool[i];
            if (!go || !go.activeSelf) continue;

            Harden(go);

            // 修正：描画Layerを壊さない。Colliderが付いているGOのみLayer変更
            SetLayerForCollidersOnly(go.transform, boatLayer);

            Vector3 l = WorldToLocal3(go.transform.position);
            l.x = Mathf.Clamp(l.x, lateralRange.x, lateralRange.y);
            l.y = Mathf.Clamp(l.y, verticalRange.x, verticalRange.y);
            l.z = Mathf.Clamp(l.z, _pLo, _pHi);
            go.transform.position = Local3ToWorld(l);

            float cruise = Random.Range(speedMin, speedMax);
            _tgt[i] = cruise;
            _cur[i] = cruise * 0.7f;
            _bias[i] = (Random.value < 0.5f) ? -1f : 1f;

            _active[i] = true;
            _activeCount++;
            _edgeHolding[i] = false;
            if (!allowLateralMovement) _fixedLat[i] = l.x;
        }
    }

    private void ValidatePool()
    {
        if (pool == null) { Debug.LogError("[BoatManager] pool が null"); return; }
        int n = pool.Length;
        int valid = 0;
        for (int i = 0; i < n; i++)
        {
            GameObject gi = pool[i];
            if (gi == null) { Debug.LogError("[BoatManager] pool[" + i + "] が null"); continue; }

            for (int j = i + 1; j < n; j++)
            {
                GameObject gj = pool[j];
                if (gj != null && gj == gi)
                    Debug.LogError("[BoatManager] pool[" + i + "] と pool[" + j + "] が同じ参照: " + gi.name);
            }

            if (gi.GetComponent<Collider>() == null)
                Debug.LogWarning("[BoatManager] " + gi.name + " に Collider が見つからない（TriggerはOFFで付けろ）");

            valid++;
        }
        Debug.Log("[BoatManager] Pool検証OK: " + valid + " エントリ");
    }

    private void ResolveAxisRanges()
    {
        if (driveAxis == Axis.X)
        {
            float a = startX, b = goalX; if (b < a) { float t = a; a = b; b = t; }
            _pLo = a; _pHi = b;
            _sign = (goalX - startX) >= 0f ? 1 : -1;
        }
        else if (driveAxis == Axis.Y)
        {
            float a = startY, b = goalY; if (b < a) { float t = a; a = b; b = t; }
            _pLo = a; _pHi = b;
            _sign = (goalY - startY) >= 0f ? 1 : -1;
        }
        else
        {
            float a = startZ, b = goalZ; if (b < a) { float t = a; a = b; b = t; }
            _pLo = a; _pHi = b;
            _sign = (goalZ - startZ) >= 0f ? 1 : -1;
        }
        if (_pHi - _pLo < 1e-4f)
        {
            _pHi = _pLo + 1f;
            Debug.LogError("[BoatManager] Start/Goal が近すぎます。値を離してください。");
        }
        if (_sign == 0) _sign = 1;
    }

    private void NormalizeAll()
    {
        if (lateralRange.y < lateralRange.x) { float t = lateralRange.x; lateralRange.x = lateralRange.y; lateralRange.y = t; }
        if (verticalRange.y < verticalRange.x) { float t = verticalRange.x; verticalRange.x = verticalRange.y; verticalRange.y = t; }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        NormalizeAll();
        ResolveAxisRanges();
        if (failSafeMargin < 0f) failSafeMargin = 0f;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || onlyWhenSelected) return;
        DrawCorridorGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || !onlyWhenSelected) return;
        DrawCorridorGizmos();
    }

    private void DrawCorridorGizmos()
    {
        NormalizeAll();
        ResolveAxisRanges();

        if (driveAxis == Axis.X)
        {
            Vector2 yr = verticalRange; Vector2 zr = lateralRange;
            Vector3 p000 = new Vector3(_pLo, yr.x, zr.x);
            Vector3 p100 = new Vector3(_pHi, yr.x, zr.x);
            Vector3 p110 = new Vector3(_pHi, yr.x, zr.y);
            Vector3 p010 = new Vector3(_pLo, yr.x, zr.y);
            Vector3 p001 = new Vector3(_pLo, yr.y, zr.x);
            Vector3 p101 = new Vector3(_pHi, yr.y, zr.x);
            Vector3 p111 = new Vector3(_pHi, yr.y, zr.y);
            Vector3 p011 = new Vector3(_pLo, yr.y, zr.y);
            DrawBox(p000, p100, p110, p010, p001, p101, p111, p011);
            Vector3 a = new Vector3(_pLo, (yr.x + yr.y) * 0.5f, (zr.x + zr.y) * 0.5f);
            Vector3 b = new Vector3(_pHi, (yr.x + yr.y) * 0.5f, (zr.x + zr.y) * 0.5f);
            DrawCenterLine(a, b);
        }
        else if (driveAxis == Axis.Y)
        {
            Vector2 xr = lateralRange; Vector2 zr = verticalRange;
            Vector3 p000 = new Vector3(xr.x, _pLo, zr.x);
            Vector3 p100 = new Vector3(xr.y, _pLo, zr.x);
            Vector3 p110 = new Vector3(xr.y, _pLo, zr.y);
            Vector3 p010 = new Vector3(xr.x, _pLo, zr.y);
            Vector3 p001 = new Vector3(xr.x, _pHi, zr.x);
            Vector3 p101 = new Vector3(xr.y, _pHi, zr.x);
            Vector3 p111 = new Vector3(xr.y, _pHi, zr.y);
            Vector3 p011 = new Vector3(xr.x, _pHi, zr.y);
            DrawBox(p000, p100, p110, p010, p001, p101, p111, p011);
            Vector3 a = new Vector3((xr.x + xr.y) * 0.5f, _pLo, (zr.x + zr.y) * 0.5f);
            Vector3 b = new Vector3((xr.x + xr.y) * 0.5f, _pHi, (zr.x + zr.y) * 0.5f);
            DrawCenterLine(a, b);
        }
        else // Z
        {
            Vector2 xr = lateralRange; Vector2 yr = verticalRange;
            Vector3 p000 = new Vector3(xr.x, yr.x, _pLo);
            Vector3 p100 = new Vector3(xr.y, yr.x, _pLo);
            Vector3 p110 = new Vector3(xr.y, yr.x, _pHi);
            Vector3 p010 = new Vector3(xr.x, yr.x, _pHi);
            Vector3 p001 = new Vector3(xr.x, yr.y, _pLo);
            Vector3 p101 = new Vector3(xr.y, yr.y, _pLo);
            Vector3 p111 = new Vector3(xr.y, yr.y, _pHi);
            Vector3 p011 = new Vector3(xr.x, yr.y, _pHi);
            DrawBox(p000, p100, p110, p010, p001, p101, p111, p011);
            Vector3 a = new Vector3((xr.x + xr.y) * 0.5f, (yr.x + yr.y) * 0.5f, _pLo);
            Vector3 b = new Vector3((xr.x + xr.y) * 0.5f, (yr.x + yr.y) * 0.5f, _pHi);
            DrawCenterLine(a, b);
        }
    }

    private void DrawBox(Vector3 p000, Vector3 p100, Vector3 p110, Vector3 p010,
                         Vector3 p001, Vector3 p101, Vector3 p111, Vector3 p011)
    {
        Gizmos.color = gizmoBoxColor;
        // 下段
        Gizmos.DrawLine(p000, p100); Gizmos.DrawLine(p100, p110);
        Gizmos.DrawLine(p110, p010); Gizmos.DrawLine(p010, p000);
        // 上段
        Gizmos.DrawLine(p001, p101); Gizmos.DrawLine(p101, p111);
        Gizmos.DrawLine(p111, p011); Gizmos.DrawLine(p011, p001);
        // 柱
        Gizmos.DrawLine(p000, p001); Gizmos.DrawLine(p100, p101);
        Gizmos.DrawLine(p110, p111); Gizmos.DrawLine(p010, p011);
    }

    private void DrawCenterLine(Vector3 a, Vector3 b)
    {
        Gizmos.color = gizmoLineColor;
        Gizmos.DrawLine(a, b);
    }
#endif
}
