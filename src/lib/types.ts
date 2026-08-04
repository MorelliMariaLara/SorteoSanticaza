export type RaffleStatus = "draft" | "active" | "closed" | "drawn";
export type OrderStatus = "pending" | "paid" | "cancelled" | "expired";

export interface Raffle {
  id: number;
  title: string;
  subtitle: string;
  description: string;
  prize_title: string;
  prize_description: string;
  draw_at: string;
  status: RaffleStatus;
  total_tickets: number;
  ticket_start: number;
  video_url: string | null;
  image_url: string | null;
  created_at: string;
}

export interface Package {
  id: number;
  raffle_id: number;
  chances: number;
  price_cents: number;
  label: string;
  popular: number;
  sort_order: number;
  active: number;
}

export interface Order {
  id: number;
  public_id: string;
  raffle_id: number;
  package_id: number;
  first_name: string;
  last_name: string;
  dni: string;
  birth_date: string;
  email: string;
  phone: string;
  chances: number;
  amount_cents: number;
  status: OrderStatus;
  payment_ref: string | null;
  created_at: string;
  paid_at: string | null;
}

export interface Ticket {
  id: number;
  raffle_id: number;
  order_id: number;
  number: number;
  created_at: string;
}

export interface Winner {
  id: number;
  raffle_id: number;
  ticket_number: number;
  prize_label: string;
  winner_name: string;
  drawn_at: string;
}

export interface RafflePublic {
  id: number;
  title: string;
  subtitle: string;
  description: string;
  prizeTitle: string;
  prizeDescription: string;
  drawAt: string;
  status: RaffleStatus;
  totalTickets: number;
  soldTickets: number;
  remainingTickets: number;
  videoUrl: string | null;
  imageUrl: string | null;
  packages: PackagePublic[];
}

export interface PackagePublic {
  id: number;
  chances: number;
  priceCents: number;
  label: string;
  popular: boolean;
}

export interface CreateOrderInput {
  packageId: number;
  firstName: string;
  lastName: string;
  dni: string;
  birthDate: string;
  email: string;
  phone: string;
  acceptTerms: boolean;
}
