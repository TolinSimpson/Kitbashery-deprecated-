using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VersionChecker : MonoBehaviour
{
    public TMP_Text version;

    // Start is called before the first frame update
    void Start()
    {
        version.text = Application.productName + " v." + Application.version;
    }
}
