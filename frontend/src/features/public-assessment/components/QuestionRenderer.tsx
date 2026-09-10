import type { PublicScaleLabel } from '@/shared/api/types';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';
import { LikertQuestion } from './LikertQuestion';
import { TextQuestion } from './TextQuestion';
import { LongTextQuestion } from './LongTextQuestion';
import { MultiChoiceQuestion } from './MultiChoiceQuestion';

/** Barcha savol komponentlari kutgan yagona javob shartnomasi (`docs/18` §6.2). */
export interface QuestionAnswerPayload {
  value?: number;
  text?: string;
  selectedValues?: number[];
}

export interface QuestionRendererProps {
  question: BranchingQuestion;
  /** `Likert5`/`Likert7`/`Binary`/`SingleChoice`/`ForcedChoice` uchun — boshqa turlarda e'tiborsiz qoldiriladi. */
  scaleLabels: readonly PublicScaleLabel[];
  /** Joriy sonli javob (Likert/Binary/SingleChoice/ForcedChoice). */
  currentValue: number | null;
  /** Joriy matn javobi (ShortText/LongText/Phone). */
  currentText: string | null;
  /** Joriy tanlangan qiymatlar (MultiChoice). */
  currentSelectedValues: readonly number[];
  invalid: boolean;
  onAnswer: (payload: QuestionAnswerPayload) => void;
  onAdvance: () => void;
  questionRef?: (node: HTMLDivElement | null) => void;
}

/**
 * `question.type` bo'yicha mos komponentni tanlaydi (`docs/18` §6.2 jadvali) — eski
 * (`Likert5`/`Likert7`/`Binary`/`SingleChoice`/`ForcedChoice`) turlar `LikertQuestion`ga,
 * yangi turlar (`ShortText`/`Phone`/`LongText`/`MultiChoice`) mos yangi komponentga boradi.
 * Barcha komponentlar bitta yagona `onAnswer` shartnomasiga moslashtiriladi — chaqiruvchi
 * (kelgusi to'lqinda `TestPage`) faqat shu bitta funksiyani biladi.
 */
export function QuestionRenderer({
  question,
  scaleLabels,
  currentValue,
  currentText,
  currentSelectedValues,
  invalid,
  onAnswer,
  onAdvance,
  questionRef,
}: QuestionRendererProps) {
  switch (question.type) {
    case 'ShortText':
    case 'Phone':
      return (
        <TextQuestion
          question={question}
          value={currentText}
          invalid={invalid}
          onAnswer={(payload) => {
            onAnswer({ text: payload.text });
          }}
          onAdvance={onAdvance}
          questionRef={questionRef}
        />
      );
    case 'LongText':
      return (
        <LongTextQuestion
          question={question}
          value={currentText}
          invalid={invalid}
          onAnswer={(payload) => {
            onAnswer({ text: payload.text });
          }}
          questionRef={questionRef}
        />
      );
    case 'MultiChoice':
      return (
        <MultiChoiceQuestion
          question={question}
          value={currentSelectedValues}
          invalid={invalid}
          onAnswer={(payload) => {
            onAnswer({ selectedValues: payload.selectedValues });
          }}
          questionRef={questionRef}
        />
      );
    default:
      // `Likert5`/`Likert7`/`Binary`/`SingleChoice`/`ForcedChoice` — mavjud xatti-harakat
      // O'ZGARISHSIZ (`LikertQuestion`ga tegilmagan, `docs/18` §1 "hech narsa buzilmaydi").
      return (
        <LikertQuestion
          question={question}
          scaleLabels={scaleLabels}
          value={currentValue}
          invalid={invalid}
          onAnswer={(value) => {
            onAnswer({ value });
          }}
          onAdvance={onAdvance}
          questionRef={questionRef}
        />
      );
  }
}
