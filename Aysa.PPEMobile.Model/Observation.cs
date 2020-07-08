﻿using System;

namespace Aysa.PPEMobile.Model
{
    public class Observation
    {
		public string Id { get; set; }

		public string Observacion { get; set; }

		public DateTime Fecha { get; set; }

		public User Usuario { get; set; }

        public Section Sector { get; set; }

        public Event Evento { get; set; }
    }
}
