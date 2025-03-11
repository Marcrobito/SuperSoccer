using System.IO;
using UnityEngine;
using System;  // Asegúrate de importar este namespace
using System.Collections.Generic;

public class JSONLoader : MonoBehaviour
{
    private string jsonPath;
    private QuestionList questionList;

    /*void Start()
    {
        // Ruta del archivo JSON en el escritorio
        //jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Soccer/questions.json");
        Debug.LogError($"El archivo JSON no se encontró en la ruta: {jsonPath}");
    }*/

    // Función para cargar y parsear el JSON
    public List<Question> LoadQuestionsFromJSON()
    {
        jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Soccer/questions.json");
        if (File.Exists(jsonPath))
        {
            string jsonContent = File.ReadAllText(jsonPath);
            questionList = JsonUtility.FromJson<QuestionList>(jsonContent);
            ShuffleQuestions();
            return questionList.questions.GetRange(0, 10);
        }
        else
        {
            Debug.LogError($"El archivo JSON no se encontró en la ruta: {jsonPath}");
            return null;
        }
    }

    private void ShuffleQuestions()
    {
        System.Random rng = new System.Random();
        int n = questionList.questions.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Question temp = questionList.questions[k];
            questionList.questions[k] = questionList.questions[n];
            questionList.questions[n] = temp;
        }
    }
}