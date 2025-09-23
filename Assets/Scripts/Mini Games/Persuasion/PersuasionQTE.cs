using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class PersuasionQTE : MonoBehaviour {
    [Header("References")]
    public RectTransform ringCenter;
    public RectTransform targeter;
    public Image safeZone;

    [Header("Tuning")]
    public float radius = 140.5f;           // How far from the center the targeter sits
    public float speed = 180f;              // How fast the targeter rotates (in degrees/sec)
    [Range(5f, 100f)]
    public float safeZoneSize = 40f;

    [Header("Targeter")]
    public float rotationOffset = 0f;       // Extra rotation applied to the targeter itself (default zero)

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private float _currentTargetAngle;      // Where the targeter currently is around the ring
    private int _direction = 1;             // 1 = CCW, -1 = CW
    private float _safeZoneCenterAngle;     // The safe zone center's angle

    private const float hitTolerance = 0.75f;  // Constant angular tolerance measured in degrees

    void Start() {
        _currentTargetAngle = Random.Range(0f, 360f);
        NewRound(false);
    }

    void Update() {
        // Spin the targeter
        _currentTargetAngle = WrapAngle(_currentTargetAngle + _direction * speed * Time.deltaTime);

        // Place and rotate the targeter
        targeter.anchoredPosition = AngleToPos(_currentTargetAngle, radius);
        targeter.localEulerAngles = new Vector3(0f, 0f, _currentTargetAngle + rotationOffset);

        // On left click, check if successful and start a new round.
        if (Input.GetMouseButtonDown(0)) {
            bool success = IsInsideSafeZone(_currentTargetAngle, _safeZoneCenterAngle, safeZoneSize);
            if (success) onSuccess?.Invoke(); else onFail?.Invoke();
            NewRound(false);
        }
    }

    void NewRound(bool preservedDirection) {
        // If preservedDirection is false, flip the direction of the targeter
        if (!preservedDirection) _direction *= -1;

        // Randomize the new safe zone's center and place it
        _safeZoneCenterAngle = Random.Range(0f, 360f);
        ShowSafeZone(_safeZoneCenterAngle, safeZoneSize);
    }
    void ShowSafeZone(float centerLocal, float sizeDeg) {
        if (!safeZone) 
            return;

        // Set how big the wedge looks
        safeZone.fillAmount = Mathf.Clamp01(sizeDeg / 360f);

        // Compute the wedge's start angle and apply it by rotating the image.
        float startLocal = centerLocal - sizeDeg * 0.5f;
        safeZone.rectTransform.localEulerAngles = new Vector3(0, 0, startLocal);
    }

    bool IsInsideSafeZone(float angleLocal, float centerLocal, float sizeDeg) {
        float sizeOnScreen = Mathf.Clamp(safeZone.fillAmount * 360f, 0f, 360f);         // The actual wedge size currently visible

        float wedgeStart = WrapAngle(safeZone.rectTransform.eulerAngles.z);             // The wedge's start angle (in world space)

        Vector3 wedgeCenter = SafeZoneWorldCenter();                                    // The wedge's center point

        float targeterAngle = AngleAround(wedgeCenter, TargeterWorldPos());             // The targeter's world space angle around the wedge's center

        float targeterHalf = TargeterHalfSpanDegreesWorld(targeterAngle, wedgeCenter);  // How many degrees of arc half the targeter spans at its current radius

        if (safeZone.fillClockwise) { // Run Unity's radial fill clockwise
            float deltaClockwise = DeltaClockwise(wedgeStart, targeterAngle);           // how far clockwise from wedge's start to the targeter
            bool hit =      // Hit if either...
                // Inside the wedge, allowing a bit of tolerance:
                deltaClockwise <= (sizeOnScreen + targeterHalf + hitTolerance) || 
                // or the wrap-around case right at the end of the wedge
                (360f - deltaClockwise) <= (targeterHalf + hitTolerance);
            return hit;

        } else { // Check for the hit the same way but run the radial fill counterclockwise
            float deltaCounterClockwise = DeltaCounterClockwise(wedgeStart, targeterAngle);
            bool hit =
                deltaCounterClockwise <= (sizeOnScreen + targeterHalf + hitTolerance) ||
                (360f - deltaCounterClockwise) <= (targeterHalf + hitTolerance);
            return hit;
        }
    }

    Vector3 SafeZoneWorldCenter() {
        var rt = safeZone.rectTransform;
        // Use the rectangle's geometric center regardless of pivot
        return rt.TransformPoint(rt.rect.center);
    }

    Vector3 TargeterWorldPos() {
        return targeter.TransformPoint(Vector3.zero); // pivot position
    }

    float AngleAround(Vector3 originWorld, Vector3 pointWorld) {
        Vector2 v = (Vector2)(pointWorld - originWorld);
        return WrapAngle(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
    }

    float TargeterHalfSpanDegreesWorld(float targetW, Vector3 originW) {
        float tr = (targetW + 90f) * Mathf.Deg2Rad;
        Vector2 tangent = new Vector2(Mathf.Cos(tr), Mathf.Sin(tr));

        Vector3 halfRightW = targeter.TransformVector(new Vector3(targeter.rect.width * 0.5f, 0f, 0f));
        Vector3 halfUpW = targeter.TransformVector(new Vector3(0f, targeter.rect.height * 0.5f, 0f));
        Vector2 hr = new Vector2(halfRightW.x, halfRightW.y);
        Vector2 hu = new Vector2(halfUpW.x, halfUpW.y);

        float projHalf = Mathf.Abs(Vector2.Dot(tangent, hr)) + Mathf.Abs(Vector2.Dot(tangent, hu));

        float radiusWorld = Mathf.Max(0.0001f, Vector3.Distance(originW, TargeterWorldPos()));

        float halfRad = Mathf.Atan2(projHalf, radiusWorld);
        return halfRad * Mathf.Rad2Deg;
    }

    static float DeltaClockwise(float start, float to) {
        float d = (start - to) % 360f;
        if (d < 0f) d += 360f;
        return d;
    }
    static float DeltaCounterClockwise(float start, float to) {
        float d = (to - start) % 360f;
        if (d < 0f) d += 360f;
        return d;
    }

    static float WrapAngle(float a) {
        a %= 360f;
        if (a < 0f) {
            a += 360f;
        }
        return a; 
    }

    static Vector2 AngleToPos(float deg, float r) {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
    }
}
