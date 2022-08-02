using System.Collections;
using UnityEngine;
using System;

public class SquareManage : MonoBehaviour
{

    [SerializeField]
    GameObject square;

    [SerializeField]
    GameObject circle;

    int count = 0;

    void Start()
    {

    }

    void Update()
    {
        count += 1;
        if (Input.GetMouseButtonDown(0)) {
            GameObject obj = Instantiate(circle);
            obj.transform.position = new Vector3(-12f, 0f, 0f);
            obj.transform.localScale = new Vector3(UnityEngine.Random.Range(1.3f, 3.0f), UnityEngine.Random.Range(1.3f, 3.0f), 1);
            obj.GetComponent<Rigidbody2D>().AddForce((Camera.main.ScreenToWorldPoint(Input.mousePosition) - obj.transform.position).normalized * 380);
        }
        if (count >= 360){
            GameObject obj = Instantiate(square);
            obj.transform.position = new Vector3(UnityEngine.Random.Range(8f, 15f), UnityEngine.Random.Range(-8f, 8f), 0);
            obj.GetComponent<Rigidbody2D>().velocity = new Vector2(-3, UnityEngine.Random.Range(-2f, 2f));
            obj.GetComponent<Rigidbody2D>().gravityScale = 0.1f;
            StartCoroutine(delay(0.1f, () =>
            {
                Vector2[] temp = obj.gameObject.transform.GetChild(0).gameObject.GetComponent<PolygonCollider2D>().points;
                for (int i = 0; i < temp.Length; i++) {
                    if (obj.gameObject.transform.GetChild(0) == null) return;
                    temp[i] = new Vector2(temp[i].x * obj.gameObject.transform.GetChild(0).gameObject.transform.localScale.x, temp[i].y * obj.gameObject.transform.GetChild(0).gameObject.transform.localScale.y);
                }
                obj.GetComponent<PolygonCollider2D>().points = temp;
            }));
            
            count = 0;
        }
    }

    IEnumerator delay(float time, Action action) {
        yield return new WaitForSeconds(time);
        action();
    }
}
