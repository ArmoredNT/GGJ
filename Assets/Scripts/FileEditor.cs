using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Windows.Forms;

public class FileEditor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OpenFile()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "All Files (*.*)|*.*";

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string filePath = openFileDialog.FileName;
            // Do something with the selected file path
            Debug.Log("Selected file: " + filePath);
        }
    }
}
