import { useTranslation } from 'react-i18next';
import { CONTACT } from '@/shared/config/contact';
import { cn } from '@/shared/lib/cn';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { GirihStar } from '@/shared/ui/brand';
import { PageHero } from '../components/PageHero';

/**
 * Aloqa sahifasi (`/aloqa`) — P45.
 *
 * Bu yerda ATAYLAB forma yo'q: xabar yuborish uchun backend endpoint'i mavjud emas, ishlamaydigan
 * forma esa foydalanuvchini aldaydi. Shu sabab faqat haqiqiy kanallar ko'rsatiladi. Forma
 * kerak bo'lganda alohida vazifa sifatida (endpoint bilan birga) qo'shiladi.
 */

/**
 * Aloqa kanallari. Manzil ham, ko'rinadigan qiymat ham `shared/config/contact` dan keladi —
 * locale faylida faqat sarlavha va izoh matni qoladi (qiymat ikki joyda saqlanmasin).
 */
const CHANNELS = [
  // Pochta manzili eng uzun qiymat — o'z qatorini to'liq egallaydi va shriftи kichikroq,
  // aks holda tor kartada `break-word` bilan ikki-uch qatorga bo'linib ketadi.
  { key: 'email', ...CONTACT.email, external: false, wide: true },
  { key: 'phone', ...CONTACT.phone, external: false, wide: false },
  { key: 'telegram', ...CONTACT.telegram, external: true, wide: false },
] as const;

const TOPICS = ['pilot', 'methodology', 'report', 'technical'] as const;

export default function ContactPage() {
  const { t } = useTranslation();
  usePageTitle(t('marketing.contact.title'));

  return (
    <>
      <PageHero
        centered
        eyebrow={t('marketing.contact.hero.eyebrow')}
        heading={t('marketing.contact.hero.heading')}
        lead={t('marketing.contact.hero.lead')}
      />

      <div className="wrap py-16 lg:py-20">
        <div className="grid gap-6 lg:grid-cols-[1.1fr_0.9fr]">
          <ul className="grid gap-5 sm:grid-cols-2">
            {CHANNELS.map((channel) => (
              <li key={channel.key} className={channel.wide ? 'sm:col-span-2' : undefined}>
                <a
                  href={channel.href}
                  rel={channel.external ? 'noreferrer' : undefined}
                  className="card card-hover relative block overflow-hidden p-8"
                >
                  <GirihStar
                    className="pointer-events-none absolute -right-12 -bottom-12 size-36 text-line/70"
                    strokeWidth={2}
                  />
                  <span className="eyebrow block text-ink-soft">
                    {t(`marketing.contact.channels.${channel.key}.title`)}
                  </span>
                  <span
                    className={cn(
                      'font-display mt-3 block font-extrabold break-words text-firuza-700',
                      channel.wide ? 'text-lg sm:text-xl' : 'text-xl',
                    )}
                  >
                    {channel.display}
                  </span>
                  <span className="mt-3 block text-[14px] leading-relaxed text-ink-soft">
                    {t(`marketing.contact.channels.${channel.key}.text`)}
                  </span>
                </a>
              </li>
            ))}
          </ul>

          <section aria-labelledby="contact-topics-heading" className="card p-8">
            <h2 id="contact-topics-heading" className="font-display text-xl font-bold">
              {t('marketing.contact.topics.heading')}
            </h2>
            <ul className="mt-5 space-y-3">
              {TOPICS.map((key) => (
                <li key={key} className="flex gap-3 text-[15px] leading-relaxed text-ink-soft">
                  <span
                    className="mt-2 size-1.5 shrink-0 rounded-full bg-firuza-600"
                    aria-hidden="true"
                  />
                  {t(`marketing.contact.topics.items.${key}`)}
                </li>
              ))}
            </ul>
            <p className="mt-6 rounded-3xl bg-paper-deep p-5 text-[13px] leading-relaxed text-ink-soft">
              {t('marketing.contact.note')}
            </p>
          </section>
        </div>
      </div>
    </>
  );
}
