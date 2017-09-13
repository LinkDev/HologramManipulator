using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHighlightable {

    void ShowHighlight();
    void HideHighlight();
    void ChangeHighlightColor(Color newColor);
    void HighlightFace(Vector3[] pointsArray, Color highlightFaceColor, Color normalFaceColor);
}
