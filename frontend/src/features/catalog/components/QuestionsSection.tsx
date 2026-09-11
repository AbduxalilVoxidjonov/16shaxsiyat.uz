import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowDown, ArrowUp, Lock, Pencil, Plus, Power, Trash2 } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { EmptyState } from '@/shared/ui/EmptyState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Skeleton } from '@/shared/ui/Skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import { useToast } from '@/shared/ui/useToast';
import { useCatalogSectionsQuery } from '../api/useCatalogSections';
import { useCatalogQuestionsQuery } from '../api/useCatalogTestDetailQuery';
import {
  useDeleteCatalogQuestion,
  useReorderCatalogQuestions,
  useUpdateCatalogQuestion,
} from '../api/useCatalogQuestionMutations';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import { buildQuestionUpdatePayload, toQuestionFormValues } from '../model/questionPayload';
import type { CatalogQuestionItem, CatalogTestDetail, QuestionType } from '../model/types';
import { QUESTION_TYPE_VALUES } from '../model/types';
import { QuestionEditorDialog } from './QuestionEditorDialog';

export interface QuestionsSectionProps {
  test: CatalogTestDetail;
}

/**
 * `type` sxemada oddiy `string` (backend `QuestionType.ToString()`). Tanilgan qiymat bo'lsa
 * o'zbekcha nom, aks holda xom kod ko'rsatiladi — noma'lum turni "Likert (5 ball)" deb
 * atash kodning o'zidan yomonroq bo'lardi.
 */
function isKnownQuestionType(value: string): value is QuestionType {
  return (QUESTION_TYPE_VALUES as readonly string[]).includes(value);
}

/**
 * Savollar bo'limi — ko'rish + tahrirlash. Tizim metodikasida (`test.isSystem`) savol
 * qo'shish va o'chirish tugmalari UMUMAN ko'rsatilmaydi (o'chirilgan holda emas — yo'q),
 * chunki backend ularni `409 SYSTEM_TEST_LOCKED` bilan rad etadi. Tartiblash esa tizimda ham
 * ochiq (BR-8 `DisplayOrder` ni qulflamaydi).
 */
export function QuestionsSection({ test }: QuestionsSectionProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();

  const questionsQuery = useCatalogQuestionsQuery(test.id);
  // Bo'limlar bo'yicha alohida so'rov — `SectionsSection` bilan BIR XIL kalit, shu sabab
  // TanStack Query keshi tufayli qo'shimcha tarmoq chaqiruvi bo'lmaydi (`docs/18` §6.3).
  const sectionsQuery = useCatalogSectionsQuery(test.id);
  const reorderQuestions = useReorderCatalogQuestions(test.id);
  const deleteQuestion = useDeleteCatalogQuestion(test.id);
  const updateQuestion = useUpdateCatalogQuestion(test.id);
  // `docs/18` B-2: bo'lim/shart FAQAT `Survey` anketalarda — `buildQuestionUpdatePayload`
  // ga aynan shu bayroq bilan bir xil qiymat berilishi kerak (`QuestionEditorDialog`dagi
  // naqsh), aks holda `sectionCode`/`visibility` noto'g'ri (qo'shilmasligi kerak bo'lgan
  // joyda qo'shilib) yuboriladi.
  const allowBranching = test.scoringMode === 'Survey';

  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<CatalogQuestionItem | null>(null);
  const [pendingDelete, setPendingDelete] = useState<CatalogQuestionItem | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [togglingId, setTogglingId] = useState<string | null>(null);

  const questions = [...(questionsQuery.data ?? [])].sort((a, b) => a.order - b.order);
  const lastQuestion = questions[questions.length - 1];
  const nextOrder = lastQuestion ? lastQuestion.order + 1 : 1;

  function openCreate() {
    setEditing(null);
    setEditorOpen(true);
  }

  function openEdit(question: CatalogQuestionItem) {
    setEditing(question);
    setEditorOpen(true);
  }

  async function handleMove(index: number, offset: -1 | 1) {
    const current = questions[index];
    const neighbour = questions[index + offset];
    if (!current || !neighbour) return;
    try {
      await reorderQuestions.mutateAsync([
        { id: current.id, displayOrder: neighbour.order },
        { id: neighbour.id, displayOrder: current.order },
      ]);
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  }

  async function handleDelete() {
    if (!pendingDelete) return;
    setDeleteError(null);
    try {
      await deleteQuestion.mutateAsync(pendingDelete.id);
      toast.show({ variant: 'success', title: t('catalog.questionDelete.successTitle') });
      setPendingDelete(null);
    } catch (caught) {
      setDeleteError(toErrorMessage(caught));
    }
  }

  /**
   * Javob berilgan savolni O'CHIRIB bo'lmaydi (`hasAnswers`, `409 QUESTION_IN_USE`) — muqobil
   * yo'l sifatida savol qatorida to'g'ridan-to'g'ri "Faol emas" almashtirgichi bor, tahrirlash
   * oynasini ochish shart emas. `buildQuestionUpdatePayload` `Custom` testda `scale`/
   * `direction`/`weight`ni ham talab qiladi, shu sabab joriy qiymatlar `toQuestionFormValues`
   * bilan to'liq formaga aylantirilib, faqat `isActive` almashtiriladi.
   *
   * **MUHIM:** `toQuestionFormValues` `sectionCode`ni har doim `''` qaytaradi (izohiga qarang —
   * u `QuestionEditorDialog`da `question.sectionId` + `sections` ro'yxatidan HISOBLANADI).
   * Shu yerda ham AYNAN o'sha hisoblash takrorlanadi — aks holda `allowBranching` bo'lganda
   * bo'limli savolni faollikni almashtirish uni bo'limdan CHIQARIB YUBORAR edi (`sectionCode:
   * null` yuborilib).
   */
  async function handleToggleActive(question: CatalogQuestionItem) {
    setTogglingId(question.id);
    try {
      const sections = sectionsQuery.data ?? [];
      const sectionCode = question.sectionId
        ? (sections.find((section) => section.id === question.sectionId)?.code ?? '')
        : '';
      await updateQuestion.mutateAsync({
        questionId: question.id,
        payload: buildQuestionUpdatePayload(
          question,
          { ...toQuestionFormValues(question), sectionCode, isActive: !question.isActive },
          { isSystem: test.isSystem, allowBranching },
        ),
      });
      toast.show({
        variant: 'success',
        title: t(
          question.isActive
            ? 'catalog.questionToggleActive.deactivatedTitle'
            : 'catalog.questionToggleActive.activatedTitle',
        ),
      });
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    } finally {
      setTogglingId(null);
    }
  }

  return (
    <Card
      title={t('catalog.detail.questionsHeading')}
      actions={
        !test.isSystem && (
          <Button size="sm" variant="outline" onClick={openCreate}>
            <Plus size={14} aria-hidden="true" />
            {t('catalog.actions.addQuestion')}
          </Button>
        )
      }
    >
      {questionsQuery.isPending && <Skeleton className="h-48 w-full" />}
      {questionsQuery.isError && <ErrorState onRetry={() => void questionsQuery.refetch()} />}

      {!questionsQuery.isPending && !questionsQuery.isError && questions.length === 0 && (
        <EmptyState
          title={t('catalog.detail.questionsEmptyTitle')}
          description={
            test.isSystem
              ? t('catalog.detail.questionsEmptySystemDescription')
              : t('catalog.detail.questionsEmptyDescription')
          }
        />
      )}

      {/* Qo'shimcha `overflow-x-auto` o'ram YO'Q: `Table` ning O'ZI `relative w-full
          overflow-x-auto` konteyneri bilan keladi (P30-6). Ikkinchi konteyner keng mazmunni
          ikki qatlamda siljitib, `sr-only` elementlar uchun tuzatilgan "containing block"
          xatosini qaytarish xavfini tug'diradi — jadval kengaydi (savol turi + shkala nomi),
          shu sabab siljish konteyneri BITTA bo'lishi muhim. */}
      {!questionsQuery.isPending && !questionsQuery.isError && questions.length > 0 && (
        <Table aria-label={t('catalog.detail.questionsHeading')}>
          <TableHeader>
            <TableRow>
              <TableHead>#</TableHead>
              <TableHead>{t('catalog.questionsTable.text')}</TableHead>
              <TableHead>{t('catalog.questionsTable.type')}</TableHead>
              <TableHead>{t('catalog.questionsTable.scale')}</TableHead>
              <TableHead>{t('catalog.questionsTable.direction')}</TableHead>
              <TableHead>{t('catalog.questionsTable.weight')}</TableHead>
              <TableHead>{t('catalog.questionsTable.state')}</TableHead>
              <TableHead>{t('catalog.questionsTable.actions')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {questions.map((question, index) => (
              <TableRow key={question.id}>
                <TableCell>{question.order}</TableCell>
                <TableCell>{question.textUz}</TableCell>
                <TableCell className="whitespace-nowrap text-neutral-500">
                  {isKnownQuestionType(question.type)
                    ? t(`catalog.questionType.${question.type}`)
                    : question.type}
                </TableCell>
                {/* Nom ASOSIY, kod ikkinchi darajali — lekin kod OLIB TASHLANMAYDI: import
                    formati va savol tahrirlash aynan kod bo'yicha ishlaydi, admin uni ko'rishi
                    kerak. `scaleNameUz` `null` bo'lsa (noma'lum kod) faqat kod ko'rinadi. */}
                <TableCell className="whitespace-nowrap">
                  {question.scaleNameUz ? (
                    <>
                      <span className="block text-neutral-900">{question.scaleNameUz}</span>
                      <span className="block text-xs text-neutral-400">{question.scale}</span>
                    </>
                  ) : (
                    <span className="text-neutral-500">{question.scale}</span>
                  )}
                </TableCell>
                <TableCell>
                  <Badge variant={question.direction === 1 ? 'neutral' : 'warning'}>
                    {question.direction === 1
                      ? t('catalog.questionsTable.directionForward')
                      : t('catalog.questionsTable.directionReverse')}
                  </Badge>
                </TableCell>
                <TableCell className="text-neutral-500">{question.weight}</TableCell>
                <TableCell>
                  <div className="flex flex-wrap items-center gap-1.5">
                    <Badge variant={question.isActive ? 'success' : 'neutral'}>
                      {question.isActive ? t('catalog.badge.active') : t('catalog.badge.inactive')}
                    </Badge>
                    {/* Tezkor almashtirgich — javob berilgan savolni o'chirib bo'lmaganda
                        (`hasAnswers`) foydalanuvchi tahrirlash oynasini ochmasdan shu yerdan
                        "Faol emas" qilib qo'ya oladi (topshiriq §3, muqobil yo'l). */}
                    <Button
                      variant="ghost"
                      size="sm"
                      aria-label={t('catalog.actions.toggleQuestionActiveAria', {
                        code: question.code,
                      })}
                      isLoading={togglingId === question.id && updateQuestion.isPending}
                      onClick={() => void handleToggleActive(question)}
                    >
                      <Power size={14} aria-hidden="true" />
                    </Button>
                  </div>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-1">
                    <Button
                      variant="ghost"
                      size="sm"
                      aria-label={t('catalog.actions.editQuestionAria', {
                        code: question.code,
                      })}
                      onClick={() => {
                        openEdit(question);
                      }}
                    >
                      <Pencil size={14} aria-hidden="true" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      aria-label={t('catalog.actions.moveUpAria', { code: question.code })}
                      disabled={index === 0 || reorderQuestions.isPending}
                      onClick={() => void handleMove(index, -1)}
                    >
                      <ArrowUp size={14} aria-hidden="true" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      aria-label={t('catalog.actions.moveDownAria', { code: question.code })}
                      disabled={index === questions.length - 1 || reorderQuestions.isPending}
                      onClick={() => void handleMove(index, 1)}
                    >
                      <ArrowDown size={14} aria-hidden="true" />
                    </Button>
                    {!test.isSystem && (
                      // `hasAnswers` bo'lsa tugma OLDINDAN bloklanadi (`409 QUESTION_IN_USE`ni
                      // bosgandan keyin ko'rish o'rniga) — sabab ham vizual (qulf belgisi), ham
                      // `title` (hover), ham `aria-label` (skrinrider) orqali beriladi, faqat
                      // `title`ga tayanilmaydi.
                      <Button
                        variant="ghost"
                        size="sm"
                        disabled={question.hasAnswers}
                        title={
                          question.hasAnswers
                            ? t('catalog.questionsTable.hasAnswersHint')
                            : undefined
                        }
                        aria-label={
                          question.hasAnswers
                            ? t('catalog.actions.deleteQuestionDisabledAria', {
                                code: question.code,
                              })
                            : t('catalog.actions.deleteQuestionAria', {
                                code: question.code,
                              })
                        }
                        onClick={() => {
                          setDeleteError(null);
                          setPendingDelete(question);
                        }}
                      >
                        {question.hasAnswers ? (
                          <Lock size={14} className="text-neutral-400" aria-hidden="true" />
                        ) : (
                          <Trash2 size={14} className="text-danger-600" aria-hidden="true" />
                        )}
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {editorOpen && (
        <QuestionEditorDialog
          open
          testId={test.id}
          question={editing}
          isSystem={test.isSystem}
          isPublished={test.status === 'Published'}
          nextOrder={nextOrder}
          scoringMode={test.scoringMode}
          sections={sectionsQuery.data ?? []}
          allQuestions={questions}
          onClose={() => {
            setEditorOpen(false);
          }}
        />
      )}

      {pendingDelete && (
        <ConfirmDialog
          open
          title={t('catalog.questionDelete.title')}
          description={pendingDelete.textUz}
          warning={t('catalog.questionDelete.warning')}
          confirmLabel={t('catalog.actions.deleteQuestion')}
          isConfirming={deleteQuestion.isPending}
          error={deleteError ?? undefined}
          onClose={() => {
            setPendingDelete(null);
            setDeleteError(null);
          }}
          onConfirm={() => void handleDelete()}
        />
      )}
    </Card>
  );
}
