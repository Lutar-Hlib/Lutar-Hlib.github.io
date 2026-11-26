const quiz = document.getElementById("quiz");
const quizResult = document.getElementById("quiz-result");

if (quiz && quizResult) {
  const questions = [...quiz.querySelectorAll(".quiz-question")];

  const clearMarks = () => {
    questions.forEach((question) => {
      question.classList.remove("is-correct", "is-wrong");
      question.querySelectorAll(".quiz-option").forEach((option) => {
        option.classList.remove("is-right-answer", "is-chosen-wrong");
      });
      question.querySelector(".quiz-feedback").hidden = true;
      question.querySelector(".quiz-explain").hidden = true;
    });
    quizResult.textContent = "";
    quizResult.classList.remove("is-visible");
  };

  quiz.addEventListener("submit", (event) => {
    event.preventDefault();
    clearMarks();

    let score = 0;
    let unanswered = 0;

    questions.forEach((question) => {
      const correct = question.dataset.answer;
      const chosen = question.querySelector("input:checked");
      const feedback = question.querySelector(".quiz-feedback");
      const explanation = question.querySelector(".quiz-explain");

      question.querySelectorAll("input").forEach((input) => {
        if (input.value === correct) {
          input.closest(".quiz-option").classList.add("is-right-answer");
        }
      });

      if (!chosen) {
        unanswered += 1;
        question.classList.add("is-wrong");
        feedback.textContent = "✗ Nezodpovězeno. Správná odpověď je označena.";
      } else if (chosen.value === correct) {
        score += 1;
        question.classList.add("is-correct");
        feedback.textContent = "✓ Správně.";
      } else {
        question.classList.add("is-wrong");
        chosen.closest(".quiz-option").classList.add("is-chosen-wrong");
        feedback.textContent = "✗ Špatně. Správná odpověď je označena.";
      }

      feedback.hidden = false;
      explanation.hidden = false;
    });

    let message = `Správně ${score} z ${questions.length}.`;
    if (unanswered > 0) {
      message += ` Nezodpovězené otázky: ${unanswered}.`;
    }
    if (score === questions.length) {
      message += " Skvělá práce!";
    }
    quizResult.textContent = message;
    quizResult.classList.add("is-visible");
  });

  quiz.addEventListener("reset", clearMarks);
}
