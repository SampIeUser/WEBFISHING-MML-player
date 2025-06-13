using System;
using System.Collections.Generic;
using System.Linq;

public class InstructionCombiner
{
    public List<string> CombineInstructions(List<string> instructions1, List<string> instructions2)
    {
        List<(string instruction, int startTime, int listIndex)> combinedInstructions = new List<(string, int, int)>();

        // Заполняем временные метки для первой и второй дорожки
        FillTimeline(instructions1, combinedInstructions, 0);
        FillTimeline(instructions2, combinedInstructions, 1);

        // Сортируем по времени исполнения (по 4-му параметру)
        combinedInstructions = combinedInstructions.OrderBy(x => x.startTime).ToList();

        List<string> newTrack = new List<string>();

        for (int i = 0; i < combinedInstructions.Count; i++)
        {
            var currentInstruction = combinedInstructions[i];

            // Проверяем, есть ли следующая инструкция с тем же временем исполнения
            if (i < combinedInstructions.Count - 1 && combinedInstructions[i + 1].startTime == currentInstruction.startTime)
            {
                var nextInstruction = combinedInstructions[i + 1];
                var partsCurrent = currentInstruction.instruction.Split(' ');
                var partsNext = nextInstruction.instruction.Split(' ');

                if (partsCurrent[0] == partsNext[0])
                {
                    // Одинаковые буквы: оставляем инструкцию из первого списка
                    if (currentInstruction.listIndex == 0)
                    {
                        newTrack.Add($"{partsCurrent[0]} {partsCurrent[1]} {partsCurrent[2]} {currentInstruction.startTime}");
                    }
                    else
                    {
                        newTrack.Add($"{partsNext[0]} {partsNext[1]} {partsNext[2]} {nextInstruction.startTime}");
                    }
                }
                else
                {
                    // Разные буквы: создаем инструкцию Z
                    newTrack.Add($"Z {partsCurrent[0]} {partsCurrent[1]} {partsCurrent[2]} {partsNext[0]} {partsNext[1]} {partsNext[2]} {currentInstruction.startTime}");
                }

                // Пропускаем следующую инструкцию
                i++;
            }
            else
            {
                // Добавляем инструкцию как есть
                newTrack.Add($"{currentInstruction.instruction} {currentInstruction.startTime}");
            }
        }

        // Теперь рассчитываем длительности и убираем последний элемент (время от начала)
        for (int i = 0; i < newTrack.Count; i++)
        {
            var parts = newTrack[i].Split(' ').ToList();
            int currentStartTime = int.Parse(parts[^1]); // Последний элемент - время от начала

            // Рассчитываем длительность
            int nextStartTime = (i < newTrack.Count - 1) ? int.Parse(newTrack[i + 1].Split(' ').Last()) : 0;
            int duration = (i < newTrack.Count - 1) ? nextStartTime - currentStartTime : 0;

            if (parts[0] == "Z")
            {
                parts[3] = duration.ToString(); // Заменяем 4-й элемент
            }
            else
            {
                parts[2] = duration.ToString(); // Заменяем 3-й элемент
            }

            parts.RemoveAt(parts.Count - 1); // Удаляем последний элемент (время от начала)
            newTrack[i] = string.Join(" ", parts);
        }

        return newTrack;
    }

    private void FillTimeline(List<string> instructions, List<(string instruction, int startTime, int listIndex)> timeline, int listIndex)
    {
        int currentTime = 0;

        foreach (var instruction in instructions)
        {
            var parts = instruction.Split(' ');
            int duration = int.Parse(parts[2]);
            timeline.Add((instruction, currentTime, listIndex));
            currentTime += duration; // Увеличиваем текущее время на продолжительность инструкции
        }
    }
}
