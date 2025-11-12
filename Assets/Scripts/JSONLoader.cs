using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;

public class JSONLoader : MonoBehaviour
{
    [SerializeField] private string relativeFolder = "Soccer";
    [SerializeField] private string fileName = "questions.json";
    [SerializeField] [Min(1)] private int questionsPerSession = 10;

    // Función para cargar y parsear el JSON
    public List<Question> LoadQuestionsFromJSON()
    {
        var questions = TryLoadFromDisk();

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("No se encontraron preguntas en disco. Usando preguntas por defecto.");
            questions = DefaultQuestions.Create();
        }

        ShuffleAnswers(questions);
        ShuffleQuestions(questions);

        int takeCount = Mathf.Min(questionsPerSession, questions.Count);
        return questions.GetRange(0, takeCount);
    }

    private List<Question> TryLoadFromDisk()
    {
        try
        {
            string basePath = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(basePath))
            {
                Debug.LogError("No se pudo determinar la ruta base del ejecutable.");
                return null;
            }

            string folderPath = Path.Combine(basePath, relativeFolder);
            string jsonPath = Path.Combine(folderPath, fileName);

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"No se encontró el archivo de preguntas en {jsonPath}");
                return null;
            }

            string jsonContent = File.ReadAllText(jsonPath);
            QuestionList parsed = JsonUtility.FromJson<QuestionList>(jsonContent);
            if (parsed == null || parsed.questions == null || parsed.questions.Count == 0)
            {
                Debug.LogWarning($"El archivo {jsonPath} no contiene preguntas válidas.");
                return null;
            }

            return parsed.questions;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al cargar preguntas: {ex.Message}");
            return null;
        }
    }

    private void ShuffleQuestions(List<Question> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Question temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    private void ShuffleAnswers(List<Question> questions)
    {
        System.Random rng = new System.Random();

        foreach (Question question in questions)
        {
            if (question.answers == null || question.answers.Count <= 1)
            {
                continue;
            }

            for (int i = question.answers.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                string temp = question.answers[k];
                question.answers[k] = question.answers[i];
                question.answers[i] = temp;
            }
        }
    }
}
