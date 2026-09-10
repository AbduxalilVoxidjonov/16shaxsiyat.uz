import { useTranslation } from 'react-i18next';
import { Plus, Trash2 } from 'lucide-react';
import type { VisibilityCondition, VisibilityOperator, VisibilityRule } from '@/shared/lib/visibility';
import { Button } from '@/shared/ui/Button';
import { Select } from '@/shared/ui/Select';
import {
  createDefaultCondition,
  describeVisibilityRule,
  operatorAllowsMultipleValues,
  operatorRequiresValues,
  operatorsForQuestionType,
  valueOptionsForQuestion,
  type VisibilityEditorQuestion,
} from '../model/visibilityEditorHelpers';

export interface VisibilityRuleEditorProps {
  value: VisibilityRule | null;
  onChange: (rule: VisibilityRule | null) => void;
  /**
   * Manba bo'lishi mumkin bo'lgan savollar — CHAQIRUVCHI filtrlaydi (B-4: faqat oldinroqdagi
   * savollar, `docs/18` §1). Bu komponent hech qanday qo'shimcha filtr qo'llamaydi.
   */
  availableQuestions: VisibilityEditorQuestion[];
  disabled?: boolean;
}

const MAX_CONDITIONS = 10;

/**
 * Ko'rsatish sharti muharriri — `docs/18` §6.3. Shart yo'q holatda "Shart qo'shish" tugmasi;
 * bor bo'lsa har bir shart uchun manba savol / operator (turga mos filtrlangan) / qiymat(lar)
 * (variantdan yoki shkala darajasidan tanlanadi — erkin son kiritish emas) va pastda
 * o'zbekcha jonli oldindan ko'rish (`describeVisibilityRule`).
 */
export function VisibilityRuleEditor({
  value,
  onChange,
  availableQuestions,
  disabled = false,
}: VisibilityRuleEditorProps) {
  const { t } = useTranslation();
  const noSourceQuestions = availableQuestions.length === 0;

  function updateCondition(index: number, patch: Partial<VisibilityCondition>) {
    if (!value) return;
    onChange({
      ...value,
      conditions: value.conditions.map((c, i) => (i === index ? { ...c, ...patch } : c)),
    });
  }

  function addRule() {
    const first = availableQuestions[0];
    if (!first) return;
    onChange({ match: 'All', conditions: [createDefaultCondition(first)] });
  }

  function removeRule() {
    onChange(null);
  }

  function addCondition() {
    if (!value) return;
    const first = availableQuestions[0];
    if (!first || value.conditions.length >= MAX_CONDITIONS) return;
    onChange({ ...value, conditions: [...value.conditions, createDefaultCondition(first)] });
  }

  function removeCondition(index: number) {
    if (!value) return;
    const conditions = value.conditions.filter((_, i) => i !== index);
    onChange(conditions.length > 0 ? { ...value, conditions } : null);
  }

  function changeConditionQuestion(index: number, code: string) {
    const question = availableQuestions.find((q) => q.code === code);
    if (!question) return;
    updateCondition(index, createDefaultCondition(question));
  }

  function changeConditionOperator(
    index: number,
    operator: VisibilityOperator,
    question: VisibilityEditorQuestion | undefined,
  ) {
    if (!operatorRequiresValues(operator)) {
      updateCondition(index, { operator, values: [] });
      return;
    }
    const options = valueOptionsForQuestion(question);
    const firstValue = options?.[0]?.value ?? 0;
    updateCondition(index, { operator, values: [firstValue] });
  }

  function toggleConditionValue(index: number, condition: VisibilityCondition, val: number) {
    const isMulti = operatorAllowsMultipleValues(condition.operator);
    if (!isMulti) {
      updateCondition(index, { values: [val] });
      return;
    }
    const has = condition.values.includes(val);
    const nextValues = has ? condition.values.filter((v) => v !== val) : [...condition.values, val];
    updateCondition(index, { values: nextValues });
  }

  if (!value) {
    return (
      <div className="flex flex-col gap-2">
        <h3 className="text-sm font-medium text-neutral-700">
          {t('catalog.visibilityEditor.heading')}
        </h3>
        {noSourceQuestions && (
          <p className="text-sm text-neutral-500">{t('catalog.visibilityEditor.noSourceQuestions')}</p>
        )}
        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={disabled || noSourceQuestions}
          onClick={addRule}
        >
          <Plus size={14} aria-hidden="true" />
          {t('catalog.visibilityEditor.addRule')}
        </Button>
      </div>
    );
  }

  const previewText = describeVisibilityRule(value, availableQuestions);

  return (
    <section className="flex flex-col gap-3 rounded-2xl border border-neutral-200 p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-medium text-neutral-700">{t('catalog.visibilityEditor.heading')}</h3>
        <Button type="button" variant="ghost" size="sm" disabled={disabled} onClick={removeRule}>
          <Trash2 size={14} className="text-danger-600" aria-hidden="true" />
          {t('catalog.visibilityEditor.removeRule')}
        </Button>
      </div>

      {value.conditions.length > 1 && (
        <Select
          label={t('catalog.visibilityEditor.matchLabel')}
          disabled={disabled}
          value={value.match}
          onChange={(event) => {
            onChange({ ...value, match: event.target.value === 'Any' ? 'Any' : 'All' });
          }}
          options={[
            { value: 'All', label: t('catalog.visibilityEditor.matchAll') },
            { value: 'Any', label: t('catalog.visibilityEditor.matchAny') },
          ]}
        />
      )}

      <ul className="flex flex-col gap-3">
        {value.conditions.map((condition, index) => {
          const question = availableQuestions.find((q) => q.code === condition.questionCode);
          const operators = question ? operatorsForQuestionType(question.type) : [];
          const valueOptions = valueOptionsForQuestion(question);
          const requiresValues = operatorRequiresValues(condition.operator);
          const isMulti = operatorAllowsMultipleValues(condition.operator);

          return (
            <li key={index} className="flex flex-col gap-2 rounded-lg bg-neutral-50 p-3">
              <div className="flex flex-wrap items-end gap-2">
                <div className="min-w-40 flex-1">
                  <Select
                    label={t('catalog.visibilityEditor.conditionSourceLabel')}
                    disabled={disabled}
                    value={condition.questionCode}
                    onChange={(event) => {
                      changeConditionQuestion(index, event.target.value);
                    }}
                    options={availableQuestions.map((q) => ({
                      value: q.code,
                      label: `${q.code} — ${q.textUz}`,
                    }))}
                  />
                </div>
                <div className="min-w-36">
                  <Select
                    label={t('catalog.visibilityEditor.conditionOperatorLabel')}
                    disabled={disabled || !question}
                    value={condition.operator}
                    onChange={(event) => {
                      changeConditionOperator(index, event.target.value as VisibilityOperator, question);
                    }}
                    options={operators.map((operator) => ({
                      value: operator,
                      label: t(`catalog.visibilityEditor.operator.${operator}`),
                    }))}
                  />
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={disabled}
                  aria-label={t('catalog.visibilityEditor.removeConditionAria', { index: index + 1 })}
                  onClick={() => {
                    removeCondition(index);
                  }}
                >
                  <Trash2 size={14} className="text-danger-600" aria-hidden="true" />
                </Button>
              </div>

              {requiresValues && valueOptions && (
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-medium text-neutral-700">
                    {t('catalog.visibilityEditor.conditionValueLabel')}
                  </span>
                  {isMulti ? (
                    <div className="flex flex-wrap gap-x-4 gap-y-1">
                      {valueOptions.map((option) => (
                        <label
                          key={option.value}
                          className="flex min-h-11 items-center gap-2 text-sm text-neutral-700"
                        >
                          <input
                            type="checkbox"
                            disabled={disabled}
                            checked={condition.values.includes(option.value)}
                            onChange={() => {
                              toggleConditionValue(index, condition, option.value);
                            }}
                          />
                          {option.label}
                        </label>
                      ))}
                    </div>
                  ) : (
                    <Select
                      aria-label={t('catalog.visibilityEditor.conditionValueLabel')}
                      disabled={disabled}
                      value={String(condition.values[0] ?? '')}
                      onChange={(event) => {
                        updateCondition(index, { values: [Number(event.target.value)] });
                      }}
                      options={valueOptions.map((option) => ({
                        value: String(option.value),
                        label: option.label,
                      }))}
                    />
                  )}
                </div>
              )}
            </li>
          );
        })}
      </ul>

      <div className="flex flex-wrap items-center gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={disabled || value.conditions.length >= MAX_CONDITIONS || noSourceQuestions}
          onClick={addCondition}
        >
          <Plus size={14} aria-hidden="true" />
          {t('catalog.visibilityEditor.addConditionCta')}
        </Button>
        {value.conditions.length >= MAX_CONDITIONS && (
          <span className="text-sm text-neutral-500">
            {t('catalog.visibilityEditor.maxConditionsReached')}
          </span>
        )}
      </div>

      {previewText && (
        <p className="rounded-lg bg-primary-50 p-3 text-sm text-primary-800">
          <span className="font-medium">{t('catalog.visibilityEditor.previewHeading')}:</span>{' '}
          {previewText}
        </p>
      )}
    </section>
  );
}
